// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xunit;

namespace Perspex.Styling.UnitTests
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

            Assert.Equal(new[] { PerspexProperty.UnsetValue, 1, PerspexProperty.UnsetValue }, result);
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
    }
}
