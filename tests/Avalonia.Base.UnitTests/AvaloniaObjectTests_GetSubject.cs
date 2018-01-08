// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_GetSubject
    {
        [Fact]
        public void GetSubject_Returns_Values()
        {
            var source = new Class1 { Foo = "foo" };
            var target = source.GetSubject(Class1.FooProperty);
            var result = new List<string>();

            target.Subscribe(x => result.Add(x));
            source.Foo = "bar";
            source.Foo = "baz";

            Assert.Equal(new[] { "foo", "bar", "baz" }, result);
        }

        [Fact]
        public void GetSubject_Sets_Values()
        {
            var source = new Class1 { Foo = "foo" };
            var target = source.GetSubject(Class1.FooProperty);

            target.OnNext("bar");
            Assert.Equal("bar", source.Foo);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Foo), "foodefault");

            public string Foo
            {
                get { return GetValue(FooProperty); }
                set { SetValue(FooProperty, value); }
            }
        }
    }
}
