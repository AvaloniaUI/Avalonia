using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    public class Clock : IObservable<TimeSpan>
    {
        public static Clock GlobalClock => AvaloniaLocator.Current.GetService<Clock>();

        private ClockObservable _observable;

        private IObservable<TimeSpan> _connectedObservable;

        private IDisposable _parentSubscription;

        private TimeSpan? _previousTime;
        private TimeSpan _internalTime;

        protected Clock()
        {
            _observable = new ClockObservable();
            _connectedObservable = _observable.Publish().RefCount();
        }

        public Clock(Clock parent)
            :this()
        {
            _parentSubscription = parent.Subscribe(Pulse);
        }

        public bool HasSubscriptions => _observable.HasSubscriptions;

        public TimeSpan CurrentTime { get; private set; }

        public PlayState PlayState { get; set; }

        protected void Pulse(TimeSpan systemTime)
        {
            if (!_previousTime.HasValue)
            {
                _previousTime = systemTime;
                _internalTime = TimeSpan.Zero;
            }
            else
            {
                if (PlayState == PlayState.Pause)
                {
                    _previousTime = systemTime;
                    return;
                }
                var delta = systemTime - _previousTime;
                _internalTime += delta.Value;
                _previousTime = systemTime;
            }

            _observable.Pulse(_internalTime);
            CurrentTime = _internalTime;
        }

        public IDisposable Subscribe(IObserver<TimeSpan> observer)
        {
            return _connectedObservable.Subscribe(observer);
        }

        private class ClockObservable : LightweightObservableBase<TimeSpan>
        {
            public bool HasSubscriptions { get; private set; }
            public void Pulse(TimeSpan time) => PublishNext(time);
            protected override void Initialize() => HasSubscriptions = true;
            protected override void Deinitialize() => HasSubscriptions = false;
        }
    }
}
