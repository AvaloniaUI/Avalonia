using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Controls.Platform;

[Unstable]
public class ManagedDispatcherImpl : IControlledDispatcherImpl
{
    private readonly IManagedDispatcherInputProvider? _inputProvider;
    private readonly AutoResetEvent _wakeup = new(false);
    private bool _signaled;
    private readonly object _lock = new();
    private long? _nextTimerMs;
    private Stopwatch _sw = Stopwatch.StartNew();
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
    public long Now => GetElapsedTimeMs();
    public void UpdateTimer(long? dueTimeInMs)
    {
        lock (_lock)
        {
            _nextTimerMs = dueTimeInMs;
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
                if (_nextTimerMs.HasValue && _nextTimerMs < GetElapsedTimeMs())
                {
                    fireTimer = true;
                    _nextTimerMs = null;
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

            long? nextTimerMs;
            lock (_lock)
            {
                nextTimerMs = _nextTimerMs;
            }

            if (nextTimerMs != null)
            {
                var waitFor = nextTimerMs.Value - GetElapsedTimeMs();
                if (waitFor < 1)
                    continue;
                Debug.Assert(waitFor < int.MaxValue);
                _wakeup.WaitOne((int)waitFor);
            }
            else
                _wakeup.WaitOne();
        }

        registration.Dispose();
    }

    private protected virtual long GetElapsedTimeMs() => _sw.ElapsedMilliseconds;
}
