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

        public Clock()
        {
            _observable = new ClockObservable();
            _connectedObservable = _observable.Publish().RefCount();
        }

        public bool HasSubscriptions => _observable.HasSubscriptions;

        public TimeSpan CurrentTime { get; private set; }

        public PlayState PlayState { get; set; }

        public void Pulse(long tickCount)
        {
            var time = TimeSpan.FromMilliseconds(tickCount);
            _observable.Pulse(time);
            CurrentTime = time;
        }

        public IDisposable Subscribe(IObserver<TimeSpan> observer)
        {
            return _connectedObservable.Subscribe(observer);
        }

        private class ClockObservable : LightweightObservableBase<TimeSpan>
        {
            public bool HasSubscriptions { get; private set; }
            public void Pulse(TimeSpan tickCount) => PublishNext(tickCount);
            protected override void Initialize() => HasSubscriptions = true;
            protected override void Deinitialize() => HasSubscriptions = false;
        }
    }
}
