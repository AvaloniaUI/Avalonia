using System;
using System.Threading;

using Android.OS;

using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Threading;

using App = Android.App.Application;

namespace Avalonia.Android
{
    internal sealed class AndroidThreadingInterface : IPlatformThreadingInterface
    {
        private Handler _handler;
        private static Thread s_uiThread;

        public AndroidThreadingInterface()
        {
            _handler = new Handler(App.Context.MainLooper);
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            if (interval.TotalMilliseconds < 10)
                interval = TimeSpan.FromMilliseconds(10);

            var stopped = false;
            Timer timer = null;
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
                            timer.Change(interval, Timeout.InfiniteTimeSpan);
                    }
                });
            },
            null, interval, Timeout.InfiniteTimeSpan);

            return Disposable.Create(() =>
            {
                stopped = true;
                timer.Dispose();
            });
        }

        private void EnsureInvokeOnMainThread(Action action) => _handler.Post(action);

        public void Signal(DispatcherPriority prio)
        {
            EnsureInvokeOnMainThread(() => Signaled?.Invoke(null));
        }

        public bool CurrentThreadIsLoopThread
        {
            get
            {
                if (s_uiThread != null)
                    return s_uiThread == Thread.CurrentThread;

                var isOnMainThread = OperatingSystem.IsAndroidVersionAtLeast(23)
                    ? Looper.MainLooper.IsCurrentThread
                    : Looper.MainLooper.Thread.Equals(Java.Lang.Thread.CurrentThread());
                if (isOnMainThread)
                {
                    s_uiThread = Thread.CurrentThread;
                    return true;
                }

                return false;
            }
        }
        public event Action<DispatcherPriority?> Signaled;
    }
}
