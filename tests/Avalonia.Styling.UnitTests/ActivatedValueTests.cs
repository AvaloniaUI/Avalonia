// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class ActivatedValueTests
    {
        [Fact]
        public void Should_Produce_Correct_Values()
        {
            var activator = new BehaviorSubject<bool>(false);
            var target = new ActivatedValue(activator, 1, string.Empty);
            var result = new List<object>();

            target.Subscribe(x => result.Add(x));

            activator.OnNext(true);
            activator.OnNext(false);

            Assert.Equal(new[] { AvaloniaProperty.UnsetValue, 1, AvaloniaProperty.UnsetValue }, result);
        }

        [Fact]
        public void Should_Complete_When_Activator_Completes()
        {
            var activator = new BehaviorSubject<bool>(false);
            var target = new ActivatedValue(activator, 1, string.Empty);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            activator.OnCompleted();

            Assert.True(completed);
        }

        [Fact]
        public void Should_Unsubscribe_From_Activator_When_All_Subscriptions_Disposed()
        {
            var scheduler = new TestScheduler();
            var activator1 = scheduler.CreateColdObservable<bool>();
            var activator2 = scheduler.CreateColdObservable<bool>();
            var activator = StyleActivator.And(new[] { activator1, activator2 });
            var target = new ActivatedValue(activator, 1, string.Empty);

            var subscription = target.Subscribe(_ => { });
            Assert.Equal(1, activator1.Subscriptions.Count);
            Assert.Equal(Subscription.Infinite, activator1.Subscriptions[0].Unsubscribe);

            subscription.Dispose();
            Assert.Equal(1, activator1.Subscriptions.Count);
            Assert.Equal(0, activator1.Subscriptions[0].Unsubscribe);
        }
    }
}
