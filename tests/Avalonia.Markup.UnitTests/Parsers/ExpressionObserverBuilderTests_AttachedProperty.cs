// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Data.Core;
using Xunit;
using Avalonia.Markup.Parsers;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class ExpressionObserverBuilderTests_AttachedProperty
    {
        private readonly Func<string, string, Type> _typeResolver;

        public ExpressionObserverBuilderTests_AttachedProperty()
        {
            var foo = Owner.FooProperty;
            _typeResolver = (_, name) => name == "Owner" ? typeof(Owner) : null;
        }

        [Fact]
        public async Task Should_Get_Attached_Property_Value()
        {
            var data = new Class1();
            var target = ExpressionObserverBuilder.Build(data, "(Owner.Foo)", typeResolver: _typeResolver);
            var result = await target.Take(1);

            Assert.Equal("foo", result);

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public async Task Should_Get_Attached_Property_Value_With_Namespace()
        {
            var data = new Class1();
            var target = ExpressionObserverBuilder.Build(
                data,
                "(NS:Owner.Foo)",
                typeResolver: (ns, name) => ns == "NS" && name == "Owner" ? typeof(Owner) : null);
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

            var target = ExpressionObserverBuilder.Build(data, "Next.(Owner.Foo)", typeResolver: _typeResolver);
            var result = await target.Take(1);

            Assert.Equal("bar", result);

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public void Should_Track_Simple_Attached_Value()
        {
            var data = new Class1();
            var target = ExpressionObserverBuilder.Build(data, "(Owner.Foo)", typeResolver: _typeResolver);
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

            var target = ExpressionObserverBuilder.Build(data, "Next.(Owner.Foo)", typeResolver: _typeResolver);
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
                var target = ExpressionObserverBuilder.Build(source, "(Owner.Foo)", typeResolver: _typeResolver);
                return Tuple.Create(target, new WeakReference(source));
            };

            var result = run();
            result.Item1.Subscribe(x => { });

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.Null(result.Item2.Target);
        }

        [Fact]
        public void Should_Fail_With_Attached_Property_With_Only_1_Part()
        {
            var data = new Class1();

            Assert.Throws<ExpressionParseException>(() => ExpressionObserverBuilder.Build(data, "(Owner)", typeResolver: _typeResolver));
        }

        [Fact]
        public void Should_Fail_With_Attached_Property_With_More_Than_2_Parts()
        {
            var data = new Class1();

            Assert.Throws<ExpressionParseException>(() => ExpressionObserverBuilder.Build(data, "(Owner.Foo.Bar)", typeResolver: _typeResolver));
        }

        private static class Owner
        {
            public static readonly AttachedProperty<string> FooProperty =
                AvaloniaProperty.RegisterAttached<Class1, string>(
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
