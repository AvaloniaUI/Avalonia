using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using CoreAnimation;
using Foundation;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;

namespace Perspex.iOS
{
    class PlatformThreadingInterface :  IPlatformThreadingInterface
    {
        static Stopwatch St = Stopwatch.StartNew();
        class Timer
        {
            readonly Action _tick;
            readonly TimeSpan _interval;
            TimeSpan _nextTick;

            public Timer(Action tick, TimeSpan interval)
            {
                _tick = tick;
                _interval = interval;
                _nextTick = St.Elapsed + _interval;
            }

            public void Tick(TimeSpan now)
            {
                if (now > _nextTick)
                {
                    _nextTick = now + _interval;
                    _tick();
                }
            }
        }

        readonly List<Timer> _timers = new List<Timer>();
        bool _signaled;
        readonly object _lock = new object();
        private CADisplayLink _link;
        public Action Render { get; set; }

        PlatformThreadingInterface()
        {
            // For some reason it doesn't work when I specify OnFrame method directly
            // ReSharper disable once ConvertClosureToMethodGroup
            (_link = CADisplayLink.Create(() => OnFrame())).AddToRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
        }

        private void OnFrame()
        {
            var now = St.Elapsed;
            List<Timer> timers;
            lock (_lock)
                timers = _timers.ToList();

            foreach (var timer in timers)
                timer.Tick(now);

            do
            {
                lock (_lock)
                    if (!_signaled)
                        break;
                    else
                        _signaled = false;
                Signaled?.Invoke();
            } while (false);
            Render?.Invoke();
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
        }

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            lock (_lock)
            {
                var timer = new Timer(tick, interval);
                _timers.Add(timer);
                return Disposable.Create(() =>
                {
                    lock (_lock) _timers.Remove(timer);
                });
            }
        }

        public void Signal()
        {
            lock (_lock)
                _signaled = true;
        }

        public bool CurrentThreadIsLoopThread => NSThread.Current.IsMainThread;
        public static PlatformThreadingInterface Instance { get; } = new PlatformThreadingInterface();

        public event Action Signaled;
    }
}
