using System;
using System.Threading;

namespace Avalonia.Threading;

/// <summary>
///     Representation of Dispatcher frame.
/// </summary>
public class DispatcherFrame
{
    private readonly bool _exitWhenRequested;
    private bool _continue;
    private bool _isRunning;
    private CancellationTokenSource? _cancellationTokenSource;
    
    /// <summary>
    ///     Constructs a new instance of the DispatcherFrame class.
    /// </summary>
    public DispatcherFrame() : this(true)
    {
    }

    public Dispatcher Dispatcher { get; }
    
    /// <summary>
    ///     Constructs a new instance of the DispatcherFrame class.
    /// </summary>
    /// <param name="exitWhenRequested">
    ///     Indicates whether or not this frame will exit when all frames
    ///     are requested to exit.
    ///     <p/>
    ///     Dispatcher frames typically break down into two categories:
    ///     1) Long running, general purpose frames, that exit only when
    ///        told to.  These frames should exit when requested.
    ///     2) Short running, very specific frames that exit themselves
    ///        when an important criteria is met.  These frames may
    ///        consider not exiting when requested in favor of waiting
    ///        for their important criteria to be met.  These frames
    ///        should have a timeout associated with them.
    /// </param>
    public DispatcherFrame(bool exitWhenRequested) : this(Dispatcher.UIThread, exitWhenRequested)
    {
        Dispatcher.VerifyAccess();
    }

    internal DispatcherFrame(Dispatcher dispatcher, bool exitWhenRequested)
    {
        Dispatcher = dispatcher;
        _exitWhenRequested = exitWhenRequested;
        _continue = true;
    }

    /// <summary>
    ///     Indicates that this dispatcher frame should exit.
    /// </summary>
    public bool Continue
    {
        get
        {
            // This method is free-threaded.

            // First check if this frame wants to continue.
            bool shouldContinue = _continue;
            if (shouldContinue)
            {
                // This frame wants to continue, so next check if it will
                // respect the "exit requests" from the dispatcher.
                if (_exitWhenRequested)
                {
                    Dispatcher dispatcher = Dispatcher;

                    // This frame is willing to respect the "exit requests" of
                    // the dispatcher, so check them.
                    if (dispatcher.ExitAllFramesRequested || dispatcher.HasShutdownStarted)
                    {
                        shouldContinue = false;
                    }
                }
            }

            return shouldContinue;
        }

        set
        {
            // This method is free-threaded.
            lock (Dispatcher.InstanceLock)
            {
                _continue = value;
                if (!_continue)
                    _cancellationTokenSource?.Cancel();
            }
        }
    }

    internal void Run(IControlledDispatcherImpl impl)
    {
        Dispatcher.VerifyAccess();
        
        // Since the actual platform run loop is controlled by a Cancellation token, we have an
        // outer loop that restarts the platform one in case Continue was set to true after being set to false
        while (true)
        {
            // Take the instance lock since `Continue` is changed from one too
            lock (Dispatcher.InstanceLock)
            {
                if (!Continue)
                    return;
                
                if (_isRunning)
                    throw new InvalidOperationException("This frame is already running");

                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;
            }

            try
            {
                // Wake up the dispatcher in case it has pending jobs
                Dispatcher.RequestProcessing();
                impl.RunLoop(_cancellationTokenSource.Token);
            }
            finally
            {
                lock (Dispatcher.InstanceLock)
                {
                    _isRunning = false;
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }
    }
    

    internal void MaybeExitOnDispatcherRequest()
    {
        if (_exitWhenRequested)
            _cancellationTokenSource?.Cancel();
    }
}