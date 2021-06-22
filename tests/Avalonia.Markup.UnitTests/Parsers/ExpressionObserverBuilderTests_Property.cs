using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Markup.Parsers;
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
                    new MissingMemberException("Could not find a matching property accessor for 'Baz' on '1'"), BindingErrorType.Error),
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

        [Fact]
        public void Should_Update_Value_After_Root_Changes()
        {
            var root = new { DataContext = new { Value = "Foo" } };
            var subject = new Subject<object>();
            var obs = ExpressionObserverBuilder.Build(subject, "DataContext.Value");

            var values = new List<object>();
            obs.Subscribe(v => values.Add(v));

            subject.OnNext(root);
            subject.OnNext(null);
            subject.OnNext(root);

            Assert.Equal("Foo", values[0]);

            Assert.IsType<BindingNotification>(values[1]);
            var bn = values[1] as BindingNotification;
            Assert.Equal(AvaloniaProperty.UnsetValue, bn.Value);
            Assert.Equal(BindingErrorType.Error, bn.ErrorType);

            Assert.Equal(3, values.Count);
            Assert.Equal("Foo", values[2]);
        }

        [Fact]
        public void Should_Update_Value_After_Root_Changes_With_ComplexPath()
        {
            var root = new { DataContext = new { Foo = new { Value = "Foo" } } };
            var subject = new Subject<object>();
            var obs = ExpressionObserverBuilder.Build(subject, "DataContext.Foo.Value");

            var values = new List<object>();
            obs.Subscribe(v => values.Add(v));

            subject.OnNext(root);
            subject.OnNext(null);
            subject.OnNext(root);

            Assert.Equal("Foo", values[0]);

            Assert.IsType<BindingNotification>(values[1]);
            var bn = values[1] as BindingNotification;
            Assert.Equal(AvaloniaProperty.UnsetValue, bn.Value);
            Assert.Equal(BindingErrorType.Error, bn.ErrorType);

            Assert.Equal(3, values.Count);
            Assert.Equal("Foo", values[2]);
        }
    }
}
