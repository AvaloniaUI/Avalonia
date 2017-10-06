using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Android
{
    class AndroidThreadingInterface : IPlatformThreadingInterface
    {
        private Handler _handler;

        public AndroidThreadingInterface()
        {
            _handler = new Handler(global::Android.App.Application.Context.MainLooper);
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
            return;
        }

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
 