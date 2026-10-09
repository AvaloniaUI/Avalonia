using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Controls.Platform;

[Unstable]
public class ManagedDispatcherImpl : IControlledDispatcherImpl, IDispatcherImplWithExplicitBackgroundProcessing
{
    private readonly IManagedDispatcherInputProvider? _inputProvider;
    private readonly IWakeupEvent _wakeup;
    private bool _signaled;
    private readonly object _lock = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan? _nextTimer; 
    private readonly Thread _loopThread = Thread.CurrentThread;
    private bool _backgroundProcessingRequested;

    public interface IManagedDispatcherInputProvider
    {
        bool HasInput { get; }
        void DispatchNextInputEvent();
    }

    /// <summary>
    /// Auto-reset wake-up primitive the loop blocks on. <see cref="Set"/> may be called from any thread,
    /// <see cref="Wait"/> only from the loop thread.
    /// </summary>
    public interface IWakeupEvent
    {
        void Set();
        void Wait(TimeSpan? timeout);
    }

    private sealed class AutoResetWakeupEvent : IWakeupEvent
    {
        private readonly AutoResetEvent _event = new(false);
        public void Set() => _event.Set();
        public void Wait(TimeSpan? timeout)
        {
            if (timeout.HasValue)
                _event.WaitOne(timeout.Value);
            else
                _event.WaitOne();
        }
    }

    public ManagedDispatcherImpl(IManagedDispatcherInputProvider? inputProvider)
        : this(inputProvider, new AutoResetWakeupEvent())
    {
    }

    public ManagedDispatcherImpl(IManagedDispatcherInputProvider? inputProvider, IWakeupEvent wakeupEvent)
    {
        _inputProvider = inputProvider;
        _wakeup = wakeupEvent;
    }

    /// <summary>
    /// Raised on the loop thread right before it blocks on the wake-up event because it found nothing to do.
    /// </summary>
    public event Action? BeforeWait;

    /// <summary>
    /// Raised on the loop thread right after the wake-up event returns, before the loop re-examines its state.
    /// Also raised after a timed-out wait. Handlers are expected to enqueue dispatcher operations, not run work.
    /// </summary>
    public event Action? AfterWakeup;

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
    
    private Action? ReadyForBackgroundProcessing { get; set; }
    event Action? IDispatcherImplWithExplicitBackgroundProcessing.ReadyForBackgroundProcessing
    {
        add => ReadyForBackgroundProcessing += value;
        remove => ReadyForBackgroundProcessing -= value;
    }

    void IDispatcherImplWithExplicitBackgroundProcessing.RequestBackgroundProcessing()
    {
        lock (_lock)
        {
            _backgroundProcessingRequested = true;
            _wakeup.Set();
        }
    }
    
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
            
            bool triggerBackgroundProcessing;
            lock (_lock)
            {
                triggerBackgroundProcessing = _backgroundProcessingRequested;
                _backgroundProcessingRequested = false;
            }

            if (triggerBackgroundProcessing)
            {
                ReadyForBackgroundProcessing?.Invoke();
                continue;
            }

            TimeSpan? nextTimer;
            lock (_lock)
            {
                nextTimer = _nextTimer;
            }

            TimeSpan? waitFor = null;
            if (nextTimer != null)
            {
                waitFor = nextTimer.Value - _clock.Elapsed;
                if (waitFor.Value.TotalMilliseconds < 1)
                    continue;
            }

            BeforeWait?.Invoke();
            _wakeup.Wait(waitFor);
            AfterWakeup?.Invoke();
        }

        registration.Dispose();
    }
}
