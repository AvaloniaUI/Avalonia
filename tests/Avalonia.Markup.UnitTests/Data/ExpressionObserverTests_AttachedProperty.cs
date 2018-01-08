// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Markup.Data;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_AttachedProperty
    {
        public ExpressionObserverTests_AttachedProperty()
        {
            var foo = Owner.FooProperty;
        }

        [Fact]
        public async Task Should_Get_Attached_Property_Value()
        {
            var data = new Class1();
            var target = new ExpressionObserver(data, "(Owner.Foo)");
            var result = await target.Take(1);

            Assert.Equal("foo", result);

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public async Task Should_Get_Chained_Attached_Property_Value()
        {
            var data = new Class1
            {
                Next = new Class1
                {
                    [Owner.FooProperty] = "bar",
                }
            };

            var target = new ExpressionObserver(data, "Next.(Owner.Foo)");
            var result = await target.Take(1);

            Assert.Equal("bar", result);

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public void Should_Track_Simple_Attached_Value()
        {
            var data = new Class1();
            var target = new ExpressionObserver(data, "(Owner.Foo)");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.SetValue(Owner.FooProperty, "bar");

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public void Should_Track_Chained_Attached_Value()
        {
            var data = new Class1
            {
                Next = new Class1
                {
                    [Owner.FooProperty] = "foo",
                }
            };

            var target = new ExpressionObserver(data, "Next.(Owner.Foo)");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Next.SetValue(Owner.FooProperty, "bar");

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive()
        {
            Func<Tuple<ExpressionObserver, WeakReference>> run = () =>
            {
                var source = new Class1();
                var target = new ExpressionObserver(source, "(Owner.Foo)");
                return Tuple.Create(target, new WeakReference(source));
            };

            var result = run();
            result.Item1.Subscribe(x => { });

            GC.Collect();

            Assert.Null(result.Item2.Target);
        }

        [Fact]
        public void Should_Fail_With_Attached_Property_With_Only_1_Part()
        {
            var data = new Class1();

            Assert.Throws<ExpressionParseException>(() => new ExpressionObserver(data, "(Owner)"));
        }

        [Fact]
        public void Should_Fail_With_Attached_Property_With_More_Than_2_Parts()
        {
            var data = new Class1();

            Assert.Throws<ExpressionParseException>(() => new ExpressionObserver(data, "(Owner.Foo.Bar)"));
        }

        private static class Owner
        {
            public static readonly AttachedProperty<string> FooProperty =
                AvaloniaProperty.RegisterAttached<AvaloniaObject, string>(
                    "Foo", 
                    typeof(Owner), 
                    defaultValue: "foo");
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<Class1> NextProperty =
                AvaloniaProperty.Register<Class1, Class1>(nameof(Next));

            public Class1 Next
            {
                get { return GetValue(NextProperty); }
                set { SetValue(NextProperty, value); }
            }
        }
    }
}
