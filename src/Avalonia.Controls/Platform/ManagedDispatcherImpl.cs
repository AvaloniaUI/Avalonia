using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Controls.Platform;

[Unstable]
public class ManagedDispatcherImpl : IControlledDispatcherImpl
{
    private readonly IManagedDispatcherInputProvider? _inputProvider;
    private readonly AutoResetEvent _wakeup = new(false);
    private bool _signaled;
    private readonly object _lock = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan? _nextTimer; 
    private readonly Thread _loopThread = Thread.CurrentThread;

    public interface IManagedDispatcherInputProvider
    {
        bool HasInput { get; }
        void DispatchNextInputEvent();
    }

    public ManagedDispatcherImpl(IManagedDispatcherInputProvider? inputProvider)
    {
        _inputProvider = inputProvider;
    }

    public bool CurrentThreadIsLoopThread => _loopThread == Thread.CurrentThread;
    public void Signal()
    {
        lock (_lock)
        {
            _signaled = true;
            _wakeup.Set();
        }
    }

    public event Action? Signaled;
    public event Action? Timer;
    public long Now => _clock.ElapsedMilliseconds;
    public void UpdateTimer(long? dueTimeInMs)
    {
        lock (_lock)
        {
            _nextTimer = dueTimeInMs == null
                ? null
                : TimeSpan.FromMilliseconds(dueTimeInMs.Value);
            if (!CurrentThreadIsLoopThread)
                _wakeup.Set();
        }
    }

    public bool CanQueryPendingInput => _inputProvider != null;
    public bool HasPendingInput => _inputProvider?.HasInput ?? false;
    
    public void RunLoop(CancellationToken token)
    {
        CancellationTokenRegistration registration = default;
        if (token.CanBeCanceled) 
            registration = token.Register(() => _wakeup.Set());

        while (!token.IsCancellationRequested)
        {
            bool signaled;
            lock (_lock)
            {
                signaled = _signaled;
                _signaled = false;
            }

            if (signaled)
            {
                Signaled?.Invoke();
                continue;
            }

            bool fireTimer = false;
            lock (_lock)
            {
                if (_nextTimer < _clock.Elapsed)
                {
                    fireTimer = true;
                    _nextTimer = null;
                }
            }

            if (fireTimer)
            {
                Timer?.Invoke();
                continue;
            }

            if (_inputProvider?.HasInput == true)
            {
                _inputProvider.DispatchNextInputEvent();
                continue;
            }

            TimeSpan? nextTimer;
            lock (_lock)
            {
                nextTimer = _nextTimer;
            }

            if (nextTimer != null)
            {
                var waitFor = nextTimer.Value - _clock.Elapsed;
                if (waitFor.TotalMilliseconds < 1)
                    continue;
                _wakeup.WaitOne(waitFor);
            }
            else
                _wakeup.WaitOne();
        }

        registration.Dispose();
    }
}
