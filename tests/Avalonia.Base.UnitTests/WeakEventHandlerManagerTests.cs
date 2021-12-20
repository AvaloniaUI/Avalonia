using System;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests
{
#pragma warning disable CS0618 // Type or member is obsolete
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
        public void EventShouldBePassedToSubscriber_Reflection()
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
        public void EventShouldBePassedToSubscriber_Delegate()
        {
            bool handled = false;
            var subscriber = new Subscriber(() => handled = true);
            var source = new EventSource();
            WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(
                source,
                (s, h) => s.Event += h,
                (s, h) => s.Event -= h,
                "Event",
                subscriber.OnEvent);
            source.Fire();
            Assert.True(handled);
        }

        [Fact]
        public void EventShouldNotBeRaisedAfterUnsubscribe_Reflection()
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
        public void EventShouldNotBeRaisedAfterUnsubscribe_Delegate()
        {
            bool handled = false;
            var subscriber = new Subscriber(() => handled = true);
            var source = new EventSource();
            WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(
                source,
                (s, h) => s.Event += h,
                (s, h) => s.Event -= h,
                "Event",
                subscriber.OnEvent);

            WeakEventHandlerManager.Unsubscribe<EventArgs, Subscriber>(source, "Event",
                subscriber.OnEvent);

            source.Fire();

            Assert.False(handled);
        }

        [Fact]
        public void EventHandlerShouldNotBeKeptAlive_Reflection()
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

            static void AddCollectableSubscriber(EventSource source, string name, Action func)
            {
                WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(source, name, new Subscriber(func).OnEvent);
            }
        }

        [Fact]
        public void EventHandlerShouldNotBeKeptAlive_Delegate()
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

            static void AddCollectableSubscriber(EventSource source, string name, Action func)
            {
                WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(
                    source,
                    (s, h) => s.Event += h,
                    (s, h) => s.Event -= h,
                    "Event",
                    new Subscriber(func).OnEvent);
            }
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
