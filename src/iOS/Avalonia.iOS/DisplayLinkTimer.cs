using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Rendering;
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Avalonia.iOS
{
    class DisplayLinkTimer : IRenderTimer
    {
        private readonly CADisplayLink _link;
        public Action<TimeSpan>? Tick { get; set; }
        private Stopwatch _st = Stopwatch.StartNew();
        private NSThread? _nsTimerThread;
        private volatile bool _wakeupSent;
        private volatile bool _stopped;
        private readonly WakeupHelper _wakeupHelper;

        public DisplayLinkTimer()
        {
            _wakeupHelper = new WakeupHelper(this);
            _link = CADisplayLink.Create(OnLinkTick);
            _link.Paused = true;
            TimerThread = new Thread(() =>
            {
                _nsTimerThread = NSThread.Current;
                _link.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Common);
                NSRunLoop.Current.Run();
            });
            TimerThread.Start();
            UIApplication.Notifications.ObserveDidEnterBackground((_,__) => _link.Paused = true);
            UIApplication.Notifications.ObserveWillEnterForeground((_, __) => Start());
        }

        public Thread TimerThread { get;  }
        
        public bool RunsInBackground => true;

        public void Start()
        {
            _stopped = false;
            if (_wakeupSent)
                return;
            _wakeupSent = true;
            var thread = _nsTimerThread;
            if (thread != null)
                _wakeupHelper.PerformSelector(new Selector("doWakeup"), thread, null, false);
        }

        public void Stop()
        {
            _stopped = true;
            _link.Paused = true;
        }

        private void OnLinkTick()
        {
            Tick?.Invoke(_st.Elapsed);
        }
        
        // NSObject subclass to allow PerformSelector dispatch to the timer run loop
        private class WakeupHelper : NSObject
        {
            private readonly DisplayLinkTimer _owner;
            
            public WakeupHelper(DisplayLinkTimer owner) => _owner = owner;
            
            [Export("doWakeup")]
            public void DoWakeup()
            {
                _owner._wakeupSent = false;
                if (!_owner._stopped)
                    _owner._link.Paused = false;
            }
        }
    }
}
