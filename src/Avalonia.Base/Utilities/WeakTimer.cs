using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Utilities
{
    public class WeakTimer
    {
        public interface IWeakTimerSubscriber
        {
            bool Tick();
        }

        private readonly WeakReference<IWeakTimerSubscriber> _subscriber;
        private DispatcherTimer _timer;

        public WeakTimer(IWeakTimerSubscriber subscriber)
        {
            _subscriber = new WeakReference<IWeakTimerSubscriber>(subscriber);
            _timer = new DispatcherTimer();
            
            _timer.Tick += delegate { OnTick(); };
        }

        private void OnTick()
        {
            IWeakTimerSubscriber subscriber;
            if (!_subscriber.TryGetTarget(out subscriber) || !subscriber.Tick())
                Stop();
        }

        public TimeSpan Interval
        {
            get { return _timer.Interval; }
            set { _timer.Interval = value; }
        }

        public void Start() => _timer.Start();
        
        public void Stop() => _timer.Stop();


        public static WeakTimer StartWeakTimer(IWeakTimerSubscriber subscriber, TimeSpan interval)
        {
            var timer = new WeakTimer(subscriber) {Interval = interval};
            timer.Start();
            return timer;
        }

    }
}
