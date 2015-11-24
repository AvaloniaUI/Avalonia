using System;
using System.Collections.Generic;
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
        readonly List<Action> _timers = new List<Action>();
        bool _signaled;

        public PlatformThreadingInterface()
        {
            CADisplayLink.Create(OnFrame).AddToRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
        }

        private void OnFrame()
        {
            foreach (var timer in _timers.ToList())
            {
                timer();
            }

            if (_signaled)
            {
                _signaled = false;
                Signaled?.Invoke();
            }
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
        }

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            _timers.Add(tick);
            return Disposable.Create(() => _timers.Remove(tick));
        }

        public void Signal()
        {
            _signaled = true;
        }

        public bool CurrentThreadIsLoopThread => NSThread.Current.IsMainThread;
        public event Action Signaled;
    }
}
