using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Avalonia.Data.Core;
using Xunit;
using Avalonia.Markup.Parsers;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class ExpressionObserverTests_Lifetime
    {
        [Fact]
        public void Should_Complete_When_Source_Observable_Completes()
        {
            var source = new BehaviorSubject<object>(1);
            var target = ExpressionObserver.Create<object, object>(source, o => o);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            source.OnCompleted();

            Assert.True(completed);
        }

        [Fact]
        public void Should_Complete_When_Source_Observable_Errors()
        {
            var source = new BehaviorSubject<object>(1);
            var target = ExpressionObserver.Create<object, object>(source, o => o);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            source.OnError(new Exception());

            Assert.True(completed);
        }

        [Fact]
        public void Should_Complete_When_Update_Observable_Completes()
        {
            var update = new Subject<ValueTuple>();
            var target = ExpressionObserver.Create(() => 1, o => o, update);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            update.OnCompleted();

            Assert.True(completed);
        }

        [Fact]
        public void Should_Complete_When_Update_Observable_Errors()
        {
            var update = new Subject<ValueTuple>();
            var target = ExpressionObserver.Create(() => 1, o => o, update);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            update.OnError(new Exception());

            Assert.True(completed);
        }

        [Fact]
        public void Should_Unsubscribe_From_Source_Observable()
        {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable(
                OnNext(1, new { Foo = "foo" }));
            var target = ExpressionObserver.Create(source, o => o.Foo);
            var result = new List<object>();

            using (target.Subscribe(x => result.Add(x)))
            using (target.Subscribe(_ => { }))
            {
                scheduler.Start();
            }

            Assert.Equal(new[] { "foo" }, result);
            Assert.All(source.Subscriptions, x => Assert.NotEqual(Subscription.Infinite, x.Unsubscribe));
        }

        [Fact]
        public void Should_Unsubscribe_From_Update_Observable()
        {
            var scheduler = new TestScheduler();
            var update = scheduler.CreateColdObservable<ValueTuple>();
            var data = new { Foo = "foo" };
            var target = ExpressionObserver.Create(() => data, o => o.Foo, update);
            var result = new List<object>();

            using (target.Subscribe(x => result.Add(x)))
            using (target.Subscribe(_ => { }))
            {
                scheduler.Start();
            }

            Assert.Equal(new[] { "foo" }, result);
            Assert.All(update.Subscriptions, x => Assert.NotEqual(Subscription.Infinite, x.Unsubscribe));

            GC.KeepAlive(data);
        }

        private static Recorded<Notification<T>> OnNext<T>(long time, T value)
        {
            return new Recorded<Notification<T>>(time, Notification.CreateOnNext<T>(value));
        }
    }
}
