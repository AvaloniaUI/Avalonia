using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Data.Core;
using Xunit;
using Avalonia.Markup.Parsers;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class ExpressionObserverTests_AvaloniaProperty
    {
        public ExpressionObserverTests_AvaloniaProperty()
        {
            var foo = Class1.FooProperty;
        }

        [Fact]
        public async Task Should_Get_Simple_Property_Value()
        {
            var data = new Class1();
            var target = ExpressionObserver.Create(data, o => o.Foo);
            var result = await target.Take(1);

            Assert.Equal("foo", result);

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public async Task Should_Get_Simple_ClrProperty_Value()
        {
            var data = new Class1();
            var target = ExpressionObserver.Create(data, o => o.ClrProperty);
            var result = await target.Take(1);

            Assert.Equal("clr-property", result);
        }

        [Fact]
        public void Should_Track_Simple_Property_Value()
        {
            var data = new Class1();
            var target = ExpressionObserver.Create(data, o => o.Foo);
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.SetValue(Class1.FooProperty, "bar");

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
                var target = ExpressionObserver.Create(source, o => o.Foo);
                return Tuple.Create(target, new WeakReference(source));
            };

            var result = run();
            result.Item1.Subscribe(x => { });

            GC.Collect();

            Assert.Null(result.Item2.Target);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", defaultValue: "foo");

            public string Foo { get => GetValue(FooProperty); set => SetValue(FooProperty, value); }

            public string ClrProperty { get; } = "clr-property";
        }
    }
}
