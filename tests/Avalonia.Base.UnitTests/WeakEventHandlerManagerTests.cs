using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class WeakEventHandlerManagerTests
    {
        class EventSource
        {
            public event EventHandler<EventArgs> Event;

            public void Fire()
            {
                Event?.Invoke(this, new EventArgs());
            }
        }

        class Subscriber
        {
            private readonly Action _onEvent;

            public Subscriber(Action onEvent)
            {
                _onEvent = onEvent;
            }

            public void OnEvent(object sender, EventArgs ev)
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
            WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(source, "Event",
                subscriber.OnEvent);
            source.Fire();
            Assert.True(handled);
        }

        [Fact]
        public void EventShouldNotBeRaisedAfterUnsubscribe()
        {
            bool handled = false;
            var subscriber = new Subscriber(() => handled = true);
            var source = new EventSource();
            WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(source, "Event",
                subscriber.OnEvent);

            WeakEventHandlerManager.Unsubscribe<EventArgs, Subscriber>(source, "Event",
                subscriber.OnEvent);

            source.Fire();

            Assert.False(handled);
        }

        [Fact]
        public void EventHandlerShouldNotBeKeptAlive()
        {
            bool handled = false;
            var source = new EventSource();
            AddCollectableSubscriber(source, "Event", () => handled = true);
            for (int c = 0; c < 10; c++)
            {
                GC.Collect();
                GC.Collect(3, GCCollectionMode.Forced, true);
            }
            source.Fire();
            Assert.False(handled);
        }

        private static void AddCollectableSubscriber(EventSource source, string name, Action func)
        {
            WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(source, name, new Subscriber(func).OnEvent);
        }
    }
}
