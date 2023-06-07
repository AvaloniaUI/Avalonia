using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class WeakEventTests
    {
        class EventSource
        {
            public event EventHandler Event;

            public void Fire()
            {
                Event?.Invoke(this, new EventArgs());
            }

            public static readonly WeakEvent<EventSource, EventArgs> WeakEv = WeakEvent.Register<EventSource>(
                (t, s) => t.Event += s,
                (t, s) => t.Event -= s);
        }

        class Subscriber : IWeakEventSubscriber<EventArgs>
        {
            private readonly Action _onEvent;

            public Subscriber(Action onEvent)
            {
                _onEvent = onEvent;
            }

            public void OnEvent(object sender, WeakEvent ev, EventArgs args)
            {
                _onEvent?.Invoke();
            }
        }

        [Fact]
        public void EventShouldBePassedToSubscriber()
        {
            bool handled = false;
            var subscriber = new Subscriber(() => handled = true);
            var source = new EventSource();
            EventSource.WeakEv.Subscribe(source, subscriber);

            source.Fire();
            Assert.True(handled);
        }

 
        [Fact]
        public void EventHandlerShouldNotBeKeptAlive()
        {
            bool handled = false;
            var source = new EventSource();
            AddSubscriber(source, () => handled = true);
            for (int c = 0; c < 10; c++)
            {
                GC.Collect();
                GC.Collect(3, GCCollectionMode.Forced, true);
            }
            source.Fire();
            Assert.False(handled);
        }

        private static void AddSubscriber(EventSource source, Action func)
        {
            EventSource.WeakEv.Subscribe(source, new Subscriber(func));
        }
    }
}
