using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Tizen;
internal class TizenThreadingInterface : IPlatformThreadingInterface
{
    internal event Action? TickExecuted;
    private bool _signaled;

    internal static SynchronizationContext MainloopContext { get; set; }

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
            MainloopContext.Post(_ => action(), null);
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
