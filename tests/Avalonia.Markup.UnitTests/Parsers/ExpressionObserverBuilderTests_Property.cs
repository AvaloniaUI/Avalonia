using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Markup.Parsers;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class ExpressionObserverBuilderTests_Property
    {
        [Fact]
        public async Task Should_Return_BindingNotification_Error_For_Broken_Chain()
        {
            var data = new { Foo = new { Bar = 1 } };
            var target = Build(data, "Foo.Bar.Baz").ToObservable();
            var result = await target.Take(1);

            Assert.IsType<BindingNotification>(result);

            Assert.Equal(
                new BindingNotification(
                    new BindingChainException("Could not find a matching property accessor for 'Baz' on 'System.Int32'.", "Foo.Bar.Baz", "Baz"),
                    BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Have_Null_SourceType_For_Broken_Chain()
        {
            var data = new { Foo = new { Bar = 1 } };
            var target = Build(data, "Foo.Bar.Baz");

            Assert.Null(target.SourceType);

            GC.KeepAlive(data);
        }

        private static BindingExpression Build(object source, string path)
        {
            var r = new CharacterReader(path);
            var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
            var nodes = ExpressionNodeFactory.CreateFromAst(grammar, null, null, out _);
            return new BindingExpression(source, nodes, AvaloniaProperty.UnsetValue);
        }
    }
}
