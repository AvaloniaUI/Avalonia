using System;
using System.Collections.Generic;

namespace Avalonia.Animation.UnitTests
{
    internal class TestClock : IClock, IDisposable
    {
        private IObserver<TimeSpan> _observer;

        public PlayState PlayState { get; set; } = PlayState.Run;
        
        public void Dispose()
        {
            _observer?.OnCompleted();
        }

        public void Step(TimeSpan time)
        {
            _observer?.OnNext(time);
        }

        public IDisposable Subscribe(IObserver<TimeSpan> observer)
        {
            _observer = observer;
            return this;
        }
    }
}