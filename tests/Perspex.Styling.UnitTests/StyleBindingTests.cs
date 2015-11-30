// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xunit;

namespace Perspex.Styling.UnitTests
{
    public class StyleBindingTests
    {
        [Fact]
        public async void Should_Produce_UnsetValue_On_Activator_False()
        {
            var activator = new BehaviorSubject<bool>(false);
            var target = new StyleBinding(activator, 1, string.Empty);
            var result = await target.Take(1);

            Assert.Equal(PerspexProperty.UnsetValue, result);
        }

        [Fact]
        public async void Should_Produce_Value_On_Activator_True()
        {
            var activator = new BehaviorSubject<bool>(true);
            var target = new StyleBinding(activator, 1, string.Empty);
            var result = await target.Take(1);

            Assert.Equal(1, result);
        }

        [Fact]
        public void Should_Change_Value_On_Activator_Change()
        {
            var activator = new BehaviorSubject<bool>(false);
            var target = new StyleBinding(activator, 1, string.Empty);
            var result = new List<object>();

            target.Subscribe(x => result.Add(x));

            activator.OnNext(true);
            activator.OnNext(false);

            Assert.Equal(new[] { PerspexProperty.UnsetValue, 1, PerspexProperty.UnsetValue }, result);
        }

        [Fact]
        public void Should_Change_Value_With_Source_Observable()
        {
            var activator = new BehaviorSubject<bool>(false);
            var source = new BehaviorSubject<object>(1);
            var target = new StyleBinding(activator, source, string.Empty);
            var result = new List<object>();

            target.Subscribe(x => result.Add(x));

            activator.OnNext(true);
            source.OnNext(2);
            activator.OnNext(false);

            Assert.Equal(new[] { PerspexProperty.UnsetValue, 1, 2, PerspexProperty.UnsetValue }, result);
        }

        [Fact]
        public void Should_Complete_When_Activator_Completes()
        {
            var activator = new BehaviorSubject<bool>(false);
            var target = new StyleBinding(activator, 1, string.Empty);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);
            activator.OnCompleted();

            Assert.True(completed);
        }
    }
}
