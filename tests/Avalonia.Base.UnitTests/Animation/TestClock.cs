using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Reactive;

namespace Avalonia.Base.UnitTests.Animation
{
    internal class TestClock : IClock, IDisposable
    {
        private TimeSpan _curTime;

        private readonly List<IObserver<TimeSpan>> _observers = new();

        public PlayState PlayState { get; set; } = PlayState.Run;

        public void Dispose()
        {
            var snapshot = _observers.ToArray();
            _observers.Clear();
            foreach (var observer in snapshot)
                observer.OnCompleted();
        }

        public void Step(TimeSpan time)
        {
            var snapshot = _observers.ToArray();
            foreach (var observer in snapshot)
                observer.OnNext(time);
        }

        public void Pulse(TimeSpan time)
        {
            _curTime += time;
            var snapshot = _observers.ToArray();
            foreach (var observer in snapshot)
                observer.OnNext(_curTime);
        }

        public IDisposable Subscribe(IObserver<TimeSpan> observer)
        {
            _observers.Add(observer);
            return Disposable.Create(() => _observers.Remove(observer));
        }
    }
}
