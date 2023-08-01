﻿using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Tizen;
internal class TizenThreadingInterface : IPlatformThreadingInterface
{
    internal static SynchronizationContext MainloopContext { get; set; }

    public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
    {
        if (interval.TotalMilliseconds < 10)
            interval = TimeSpan.FromMilliseconds(10);

        var stopped = false;
        Timer? timer = null;
        timer = new Timer(_ =>
        {
            if (stopped)
                return;

            EnsureInvokeOnMainThread(() =>
            {
                try
                {
                    tick();
                }
                finally
                {
                    if (!stopped)
                        timer?.Change(interval, Timeout.InfiniteTimeSpan);
                }
            });
        },
        null, interval, Timeout.InfiniteTimeSpan);

        return Reactive.Disposable.Create(() =>
        {
            stopped = true;
            timer?.Dispose();
        });
    }

    private void EnsureInvokeOnMainThread(Action action)
    {
        if (SynchronizationContext.Current == MainloopContext)
            action();
        else
            MainloopContext.Post(_ => action(), null);
    }

    public void Signal(DispatcherPriority prio) =>
        EnsureInvokeOnMainThread(() => Signaled?.Invoke(prio));

    public bool CurrentThreadIsLoopThread => true;
    public event Action<DispatcherPriority?> Signaled;
}
