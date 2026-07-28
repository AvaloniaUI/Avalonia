using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Rendering;
using CoreAnimation;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    class DisplayLinkTimer : IRenderTimer
    {
        private volatile Action<TimeSpan>? _tick;
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

        // TODO: start/stop on RenderLoop request
        public Action<TimeSpan>? Tick
        {
            get => _tick;
            set => _tick = value;
        }

        private void OnLinkTick()
        {
            _tick?.Invoke(_st.Elapsed);
        }
    }
}
