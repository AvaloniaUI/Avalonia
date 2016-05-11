// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class ActivatedObservableTests
    {
        [Fact]
        public void Should_Produce_Correct_Values()
        {
            var activator = new BehaviorSubject<bool>(false);
            var source = new BehaviorSubject<object>(1);
            var target = new ActivatedObservable(activator, source, string.Empty);
            var result = new List<object>();

            target.Subscribe(x => result.Add(x));

            activator.OnNext(true);
            source.OnNext(2);
            activator.OnNext(false);
            source.OnNext(3);
            activator.OnNext(true);

            Assert.Equal(
                new[] 
                {
                    AvaloniaProperty.UnsetValue,
                    1,
                    2,
                    AvaloniaProperty.UnsetValue,
                    3,
                }, 
                result);
        }

        [Fact]
        public void Should_Complete_When_Source_Completes()
        {
            var activator = new BehaviorSubject<bool>(false);
            var source = new BehaviorSubject<object>(1);
            var target = new ActivatedObservable(activator, source, string.Empty);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            source.OnCompleted();

            Assert.True(completed);
        }

        [Fact]
        public void Should_Complete_When_Activator_Completes()
        {
            var activator = new BehaviorSubject<bool>(false);
            var source = new BehaviorSubject<object>(1);
            var target = new ActivatedObservable(activator, source, string.Empty);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            activator.OnCompleted();

            Assert.True(completed);
        }
    }
}
