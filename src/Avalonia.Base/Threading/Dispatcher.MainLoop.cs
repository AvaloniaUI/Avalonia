using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Utilities;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    internal bool ExitAllFramesRequested { get; private set; }
    internal bool HasShutdownStarted { get; private set; }
    internal int DisabledProcessingCount { get; set; }
    private bool _hasShutdownFinished;
    private bool _startingShutdown;
    
    private readonly Stack<DispatcherFrame> _frames = new();
    

    /// <summary>
    ///     Raised when the dispatcher is shutting down.
    /// </summary>
    public event EventHandler? ShutdownStarted;

    /// <summary>
    ///     Raised when the dispatcher is shut down.
    /// </summary>
    public event EventHandler? ShutdownFinished;

    /// <summary>
    ///     Push an execution frame.
    /// </summary>
    /// <param name="frame">
    ///     The frame for the dispatcher to process.
    /// </param>
    public void PushFrame(DispatcherFrame frame)
    {
        VerifyAccess();
        if (_controlledImpl == null)
            throw new PlatformNotSupportedException();
        _ = frame ?? throw new ArgumentNullException(nameof(frame));

        if(_hasShutdownFinished) // Dispatcher thread - no lock needed for read
            throw new InvalidOperationException("Cannot perform requested operation because the Dispatcher shut down");

        if (DisabledProcessingCount > 0)
            throw new InvalidOperationException(
                "Cannot perform this operation while dispatcher processing is suspended.");

        try
        {
            _frames.Push(frame);
            using (AvaloniaSynchronizationContext.Ensure(this, DispatcherPriority.Normal))
                frame.Run(_controlledImpl);
        }
        finally
        {
            _frames.Pop();
            if (_frames.Count == 0)
            {
                if (HasShutdownStarted)
                    ShutdownImpl();
                else
                    ExitAllFramesRequested = false;
            }
        }
    }
    
    /// <summary>
    /// Runs the dispatcher's main loop.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token used to exit the main loop.
    /// </param>
    public void MainLoop(CancellationToken cancellationToken)
    {
        if (_controlledImpl == null)
            throw new PlatformNotSupportedException();
        var frame = new DispatcherFrame();
        cancellationToken.Register(() => frame.Continue = false);
        PushFrame(frame);
    }
    
    /// <summary>
    ///     Requests that all nested frames exit.
    /// </summary>
    public void ExitAllFrames()
    {
        if (_frames.Count == 0)
            return;
        ExitAllFramesRequested = true;
        foreach (var f in _frames)
            f.MaybeExitOnDispatcherRequest();
    }
    
    /// <summary>
    ///     Begins the process of shutting down the dispatcher.
    /// </summary>
    public void BeginInvokeShutdown(DispatcherPriority priority) => Post(StartShutdownImpl, priority);

    
    /// <summary>
    /// Initiates the shutdown process of the Dispatcher synchronously.
    /// </summary>
    public void InvokeShutdown() => Invoke(StartShutdownImpl, DispatcherPriority.Send);

    private void StartShutdownImpl()
    {
        if (!_startingShutdown)
        {
            // We only need this to prevent reentrancy if the ShutdownStarted event
            // tries to shut down again.
            _startingShutdown = true;

            // Call the ShutdownStarted event before we actually mark ourselves
            // as shutting down.  This is so the handlers can actually do work
            // when they get this event without throwing exceptions.
            ShutdownStarted?.Invoke(this, EventArgs.Empty);

            HasShutdownStarted = true;
            
            if (_frames.Count > 0)
                ExitAllFrames();
            else ShutdownImpl();
        }
    }


    private void ShutdownImpl()
    {
        DispatcherOperation? operation = null;
        _impl.Timer -= PromoteTimers;
        _impl.Signaled -= Signaled;
        do
        {
            lock (InstanceLock)
            {
                if (_queue.MaxPriority != DispatcherPriority.Invalid)
                {
                    operation = _queue.Peek();
                }
                else
                {
                    operation = null;
                }
            }

            if (operation != null)
            {
                operation.Abort();
            }
        } while (operation != null);

        _impl.UpdateTimer(null);
        _hasShutdownFinished = true;
        ShutdownFinished?.Invoke(this, EventArgs.Empty);
    }

    public record struct DispatcherProcessingDisabled : IDisposable
    {
        private readonly SynchronizationContext? _oldContext;
        
        private readonly bool _restoreContext;
        private Dispatcher? _dispatcher;

        internal DispatcherProcessingDisabled(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        internal DispatcherProcessingDisabled(Dispatcher dispatcher, SynchronizationContext? oldContext) : this(
            dispatcher)
        {
            _oldContext = oldContext;
            _restoreContext = true;
        }
        
        public void Dispose()
        {
            if(_dispatcher==null)
                return;
            _dispatcher.DisabledProcessingCount--;
            _dispatcher = null;
            if (_restoreContext)
                SynchronizationContext.SetSynchronizationContext(_oldContext);
        }
    }
    
    /// <summary>
    ///     Disable the event processing of the dispatcher.
    /// </summary>
    /// <remarks>
    ///     This is an advanced method intended to eliminate the chance of
    ///     unrelated reentrancy.  The effect of disabling processing is:
    ///     1) CLR locks will not pump messages internally.
    ///     2) No one is allowed to push a frame.
    ///     3) No message processing is permitted.
    /// </remarks>
    public DispatcherProcessingDisabled DisableProcessing()
    {
        VerifyAccess();

        // Turn off processing.
        DisabledProcessingCount++;
        var oldContext = SynchronizationContext.Current;
        if (oldContext is AvaloniaSynchronizationContext or NonPumpingSyncContext)
            return new DispatcherProcessingDisabled(this);

        var helper = AvaloniaLocator.Current.GetService<NonPumpingLockHelper.IHelperImpl>();
        if (helper == null)
            return new DispatcherProcessingDisabled(this);

        SynchronizationContext.SetSynchronizationContext(new NonPumpingSyncContext(helper, oldContext));
        return new DispatcherProcessingDisabled(this, oldContext);

    }
}