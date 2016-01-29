// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Perspex.Markup.Data;
using Xunit;

namespace Perspex.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_PerspexProperty
    {
        public ExpressionObserverTests_PerspexProperty()
        {
            var foo = Class1.FooProperty;
        }

        [Fact]
        public async void Should_Get_Simple_Property_Value()
        {
            var data = new Class1();
            var target = new ExpressionObserver(data, "Foo");
            var result = await target.Take(1);

            Assert.Equal("foo", result);
        }

        [Fact]
        public void Should_Track_Simple_Property_Value()
        {
            var data = new Class1();
            var target = new ExpressionObserver(data, "Foo");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.SetValue(Class1.FooProperty, "bar");

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();
        }

        private class Class1 : PerspexObject
        {
            public static readonly StyledProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo", defaultValue: "foo");
        }
    }
}
