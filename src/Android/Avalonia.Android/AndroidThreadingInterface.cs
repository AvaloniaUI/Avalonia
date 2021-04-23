using System;
using System.Reactive.Disposables;
using System.Threading;

using Android.OS;

using Avalonia.Platform;
using Avalonia.Threading;

using App = Android.App.Application;

namespace Avalonia.Android
{
    internal sealed class AndroidThreadingInterface : IPlatformThreadingInterface
    {
        private Handler _handler;

        public AndroidThreadingInterface()
        {
            _handler = new Handler(App.Context.MainLooper);
        }

        public void RunLoop(CancellationToken cancellationToken) => throw new NotSupportedException();

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            if (interval.TotalMilliseconds < 10)
                interval = TimeSpan.FromMilliseconds(10);
            object l = new object();
            var stopped = false;
            Timer timer = null;
            var scheduled = false;
            timer = new Timer(_ =>
            {
                lock (l)
                {
                    if (stopped)
                    {
                        timer.Dispose();
                        return;
                    }
                    if (scheduled)
                        return;
                    scheduled = true;
                    EnsureInvokeOnMainThread(() =>
                    {
                        try
                        {
                            tick();
                        }
                        finally
                        {
                            lock (l)
                            {
                                scheduled = false;
                            }
                        }
                    });
                }
            }, null, TimeSpan.Zero, interval);

            return Disposable.Create(() =>
            {
                lock (l)
                {
                    stopped = true;
                    timer.Dispose();
                }
            });
        }

        private void EnsureInvokeOnMainThread(Action action) => _handler.Post(action);

        public void Signal(DispatcherPriority prio)
        {
            EnsureInvokeOnMainThread(() => Signaled?.Invoke(null));
        }

        public bool CurrentThreadIsLoopThread => Looper.MainLooper.Thread.Equals(Java.Lang.Thread.CurrentThread());
        public event Action<DispatcherPriority?> Signaled;
    }
}
