using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Tizen;
internal class TizenThreadingInterface : IPlatformThreadingInterface
{
    internal event Action? TickExecuted;
    private bool _signaled;

    private static SynchronizationContext? s_mainloopContext;

    internal static SynchronizationContext MainloopContext
    {
        get => s_mainloopContext ?? throw new InvalidOperationException($"{nameof(MainloopContext)} hasn't been set");
        set => s_mainloopContext = value;
    }

    public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
    {
        return new Timer(_ =>
        {
            EnsureInvokeOnMainThread(tick);
        }, null, interval, interval);
    }

    private void EnsureInvokeOnMainThread(Action action)
    {
        if (SynchronizationContext.Current != null)
            action();
        else
            MainloopContext.Post(static arg => ((Action)arg!).Invoke(), action);
    }

    public void Signal(DispatcherPriority prio)
    {
        if (_signaled)
            return;

        _signaled = true;
        var interval = TimeSpan.FromMilliseconds(1);

        IDisposable? disp = null;
        disp = new Timer(_ =>
        {
            _signaled = false;
            disp?.Dispose();
            
            EnsureInvokeOnMainThread(() => Signaled?.Invoke(prio));
        }, null, interval, interval);
    }

    public bool CurrentThreadIsLoopThread => SynchronizationContext.Current != null;
    public event Action<DispatcherPriority?>? Signaled;
}
