using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Markup.Parsers;
using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class ExpressionObserverBuilderTests_Method
    {
        private class TestObject
        {
            public void MethodWithoutReturn() { }

            public int MethodWithReturn() => 0;

            public int MethodWithReturnAndParameter(object i) => (int)i;

            public static void StaticMethod() { }
        }

        [Fact]
        public async Task Should_Get_Method()
        {
            var data = new TestObject();
            var observer = Build(data, nameof(TestObject.MethodWithoutReturn));
            var result = await observer.Take(1);

            Assert.NotNull(result);

            GC.KeepAlive(data);
        }

        [Theory]
        [InlineData(nameof(TestObject.MethodWithoutReturn), typeof(Action))]
        [InlineData(nameof(TestObject.MethodWithReturn), typeof(Func<int>))]
        [InlineData(nameof(TestObject.MethodWithReturnAndParameter), typeof(Func<object, int>))]
        [InlineData(nameof(TestObject.StaticMethod), typeof(Action))]
        public async Task Should_Get_Method_WithCorrectDelegateType(string methodName, Type expectedType)
        {
            var data = new TestObject();
            var observer = Build(data, methodName);
            var result = await observer.Take(1);

            Assert.IsType(expectedType, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Can_Call_Method_Returned_From_Observer()
        {
            var data = new TestObject();
            var observer = Build(data, nameof(TestObject.MethodWithReturnAndParameter));
            var result = await observer.Take(1);

            var callback = (Func<object, int>)result;

            Assert.Equal(1, callback(1));

            GC.KeepAlive(data);
        }


        private static IObservable<object> Build(object source, string path)
        {
            var r = new CharacterReader(path);
            var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
            var nodes = ExpressionNodeFactory.CreateFromAst(grammar, null, null, out _);
            return new BindingExpression(source, nodes, AvaloniaProperty.UnsetValue).ToObservable();
        }
    }
}
