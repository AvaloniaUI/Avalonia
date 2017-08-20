// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Avalonia.Markup.Data;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_Lifetime
    {
        [Fact]
        public void Should_Complete_When_Source_Observable_Completes()
        {
            var source = new BehaviorSubject<object>(1);
            var target = new ExpressionObserver(source, "Foo");
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            source.OnCompleted();

            Assert.True(completed);
        }

        [Fact]
        public void Should_Complete_When_Source_Observable_Errors()
        {
            var source = new BehaviorSubject<object>(1);
            var target = new ExpressionObserver(source, "Foo");
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            source.OnError(new Exception());

            Assert.True(completed);
        }

        [Fact]
        public void Should_Complete_When_Update_Observable_Completes()
        {
            var update = new Subject<Unit>();
            var target = new ExpressionObserver(() => 1, "Foo", update);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            update.OnCompleted();

            Assert.True(completed);
        }

        [Fact]
        public void Should_Complete_When_Update_Observable_Errors()
        {
            var update = new Subject<Unit>();
            var target = new ExpressionObserver(() => 1, "Foo", update);
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
            var target = new ExpressionObserver(source, "Foo");
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
            var update = scheduler.CreateColdObservable<Unit>();
            var data = new { Foo = "foo" };
            var target = new ExpressionObserver(() => data, "Foo", update);
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

        private Recorded<Notification<object>> OnNext(long time, object value)
        {
            return new Recorded<Notification<object>>(time, Notification.CreateOnNext<object>(value));
        }
    }
}
