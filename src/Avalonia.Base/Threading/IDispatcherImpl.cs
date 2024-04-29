using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Threading;

[PrivateApi]
public interface IDispatcherImpl
{
    bool CurrentThreadIsLoopThread { get; }

    // Asynchronously triggers Signaled callback
    void Signal();
    event Action? Signaled;
    event Action? Timer;
    long Now { get; }
    void UpdateTimer(long? dueTimeInMs);
}

[PrivateApi]
public interface IDispatcherImplWithPendingInput : IDispatcherImpl
{
    // Checks if dispatcher implementation can 
    bool CanQueryPendingInput { get; }
    // Checks if there is pending user input
    bool HasPendingInput { get; }
}

[PrivateApi]
public interface IDispatcherImplWithExplicitBackgroundProcessing : IDispatcherImpl
{
    event Action? ReadyForBackgroundProcessing;
    void RequestBackgroundProcessing();
}

[PrivateApi]
public interface IControlledDispatcherImpl : IDispatcherImplWithPendingInput
{
    // Runs the event loop
    void RunLoop(CancellationToken token);
}

internal class LegacyDispatcherImpl : IDispatcherImpl
{
    private readonly IPlatformThreadingInterface _platformThreading;
    private IDisposable? _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public LegacyDispatcherImpl(IPlatformThreadingInterface platformThreading)
    {
        _platformThreading = platformThreading;
        _platformThreading.Signaled += delegate { Signaled?.Invoke(); };
    }

    public bool CurrentThreadIsLoopThread => _platformThreading.CurrentThreadIsLoopThread;
    public void Signal() => _platformThreading.Signal(DispatcherPriority.Send);

    public event Action? Signaled;
    public event Action? Timer;
    public long Now => _clock.ElapsedMilliseconds;
    public void UpdateTimer(long? dueTimeInMs)
    {
        _timer?.Dispose();
        _timer = null;

        if (dueTimeInMs.HasValue)
        {
            var interval = Math.Max(1, dueTimeInMs.Value - _clock.ElapsedMilliseconds);
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
}

internal sealed class NullDispatcherImpl : IDispatcherImpl
{
    public bool CurrentThreadIsLoopThread => true;

    public void Signal()
    {
        
    }
    
    public event Action? Signaled
    {
        add { }
        remove { }
    }

    public event Action? Timer
    {
        add { }
        remove { }
    }

    public long Now => 0;

    public void UpdateTimer(long? dueTimeInMs)
    {
        
    }
}
