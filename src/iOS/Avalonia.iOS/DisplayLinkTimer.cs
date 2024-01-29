using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Rendering;
using CoreAnimation;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    class DisplayLinkTimer : IRenderTimer
    {
        public event Action<TimeSpan>? Tick;
        private Stopwatch _st = Stopwatch.StartNew();

        public DisplayLinkTimer()
        {
            var link = CADisplayLink.Create(OnLinkTick);
            TimerThread = new Thread(() =>
            {
                link.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Common);
                NSRunLoop.Current.Run();
            });
            TimerThread.Start();
            UIApplication.Notifications.ObserveDidEnterBackground((_,__) => link.Paused = true);
            UIApplication.Notifications.ObserveWillEnterForeground((_, __) => link.Paused = false);
        }

        public Thread TimerThread { get;  }
        
        public bool RunsInBackground => true;

        private void OnLinkTick()
        {
            Tick?.Invoke(_st.Elapsed);
        }
    }
}
