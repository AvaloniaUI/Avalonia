using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Data.Core;
using Xunit;
using Avalonia.Markup.Parsers;
using Avalonia.Utilities;
using Avalonia.Data.Core.ExpressionNodes;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class ExpressionObserverBuilderTests_AvaloniaProperty
    {
        public ExpressionObserverBuilderTests_AvaloniaProperty()
        {
            var foo = Class1.FooProperty;
        }

        [Fact]
        public async Task Should_Get_AvaloniaProperty_By_Name()
        {
            var data = new Class1();
            var target = Build(data, "Foo").ToObservable();
            var result = await target.Take(1);

            Assert.Equal("foo", result);

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public void Should_Track_AvaloniaProperty_By_Name()
        {
            var data = new Class1();
            var target = Build(data, "Foo");
            var result = new List<object>();

            var sub = target.ToObservable().Subscribe(x => result.Add(x));
            data.SetValue(Class1.FooProperty, "bar");

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        private static BindingExpression Build(object source, string path)
        {
            var r = new CharacterReader(path);
            var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
            var nodes = ExpressionNodeFactory.CreateFromAst(grammar, null, null, out _);
            return new BindingExpression(source, nodes, AvaloniaProperty.UnsetValue);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", defaultValue: "foo");

            public string ClrProperty { get; } = "clr-property";
        }
    }
}
