using Avalonia.Data;
using Avalonia.Markup.Parsers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class ExpressionObserverBuilderTests_Property
    {
        [Fact]
        public async Task Should_Return_BindingNotification_Error_For_Broken_Chain()
        {
            var data = new { Foo = new { Bar = 1 } };
            var target = ExpressionObserverBuilder.Build(data, "Foo.Bar.Baz");
            var result = await target.Take(1);

            Assert.IsType<BindingNotification>(result);

            Assert.Equal(
                new BindingNotification(
                    new MissingMemberException("Could not find CLR property 'Baz' on '1'"), BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Have_Null_ResultType_For_Broken_Chain()
        {
            var data = new { Foo = new { Bar = 1 } };
            var target = ExpressionObserverBuilder.Build(data, "Foo.Bar.Baz");

            Assert.Null(target.ResultType);

            GC.KeepAlive(data);
        }
    }
}
