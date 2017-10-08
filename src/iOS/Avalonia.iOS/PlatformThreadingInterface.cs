using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using CoreAnimation;
using Foundation;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Threading;

namespace Avalonia.iOS
{
    class PlatformThreadingInterface :  IPlatformThreadingInterface
    {
        private bool _signaled;
        public static PlatformThreadingInterface Instance { get; } = new PlatformThreadingInterface();
        public bool CurrentThreadIsLoopThread => NSThread.Current.IsMainThread;
        
        public event Action<DispatcherPriority?> Signaled;
        public void RunLoop(CancellationToken cancellationToken)
        {
            //Mobile platforms are using external main loop
            throw new NotSupportedException(); 
        }
        /*
        class Timer : NSObject
        {
            private readonly Action _tick;
            private NSTimer _timer;

            public Timer(TimeSpan interval, Action tick)
            {
                _tick = tick;
                _timer = new NSTimer(NSDate.Now, interval.TotalSeconds, true, OnTick);
            }

            [Export("onTick")]
            private void OnTick(NSTimer nsTimer)
            {
                _tick();
            }

            protected override void Dispose(bool disposing)
            {
                if(disposing)
                    _timer.Dispose();
                base.Dispose(disposing);
            }
        }*/

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
            => NSTimer.CreateRepeatingScheduledTimer(interval, _ => tick());

        public void Signal(DispatcherPriority prio)
        {
            lock (this)
            {
                if(_signaled)
                    return;
                _signaled = true;
            }
            NSRunLoop.Main.BeginInvokeOnMainThread(() =>
            {
                lock (this)
                    _signaled = false;
                Signaled?.Invoke(null);
            });
        }
    }
}
