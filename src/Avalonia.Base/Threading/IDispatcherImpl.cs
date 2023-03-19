using System;
using System.Threading;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Threading;

[Unstable]
public interface IDispatcherImpl
{
    bool CurrentThreadIsLoopThread { get; }

    // Asynchronously triggers Signaled callback
    void Signal();
    event Action Signaled;
    event Action Timer;
    void UpdateTimer(int? dueTimeInMs);
}

[Unstable]
public interface IDispatcherImplWithPendingInput : IDispatcherImpl
{
    // Checks if dispatcher implementation can 
    bool CanQueryPendingInput { get; }
    // Checks if there is pending user input
    bool HasPendingInput { get; }
}

[Unstable]
public interface IDispatcherImplWithExplicitBackgroundProcessing : IDispatcherImpl
{
    event Action ReadyForBackgroundProcessing;
    void RequestBackgroundProcessing();
}

[Unstable]
public interface IControlledDispatcherImpl : IDispatcherImplWithPendingInput
{
    // Runs the event loop
    void RunLoop(CancellationToken token);
}

internal class LegacyDispatcherImpl : DefaultDispatcherClock, IControlledDispatcherImpl
{
    private readonly IPlatformThreadingInterface _platformThreading;
    private IDisposable? _timer;

    public LegacyDispatcherImpl(IPlatformThreadingInterface platformThreading)
    {
        _platformThreading = platformThreading;
        _platformThreading.Signaled += delegate { Signaled?.Invoke(); };
    }

    public bool CurrentThreadIsLoopThread => _platformThreading.CurrentThreadIsLoopThread;
    public void Signal() => _platformThreading.Signal(DispatcherPriority.Send);

    public event Action? Signaled;
    public event Action? Timer;
    public void UpdateTimer(int? dueTimeInMs)
    {
        _timer?.Dispose();
        _timer = null;

        if (dueTimeInMs.HasValue)
        {
            var interval = Math.Max(1, dueTimeInMs.Value - TickCount);
            _timer = _platformThreading.StartTimer(DispatcherPriority.Send,
                TimeSpan.FromMilliseconds(interval),
                OnTick);
        }
    }

    private void OnTick()
    {
        _timer?.Dispose();
        _timer = null;
        Timer?.Invoke();
    }

    public bool CanQueryPendingInput => false;
    public bool HasPendingInput => false;
    public void RunLoop(CancellationToken token) => _platformThreading.RunLoop(token);
}

class NullDispatcherImpl : IDispatcherImpl
{
    public bool CurrentThreadIsLoopThread => true;

    public void Signal()
    {
        
    }
    
    public event Action? Signaled;
    public event Action? Timer;

    public void UpdateTimer(int? dueTimeInMs)
    {
        
    }
}
