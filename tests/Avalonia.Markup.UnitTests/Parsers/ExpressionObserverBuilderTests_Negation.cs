using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class ExpressionObserverBuilderTests_Negation
    {
        [Fact]
        public async Task Should_Negate_0()
        {
            var data = new { Foo = 0 };
            var target = BuildAsObservable(data, "!Foo");
            var result = await target.Take(1);

            Assert.True((bool)result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_1()
        {
            var data = new { Foo = 1 };
            var target = BuildAsObservable(data, "!Foo");
            var result = await target.Take(1);

            Assert.False((bool)result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_False_String()
        {
            var data = new { Foo = "false" };
            var target = BuildAsObservable(data, "!Foo");
            var result = await target.Take(1);

            Assert.True((bool)result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_True_String()
        {
            var data = new { Foo = "True" };
            var target = BuildAsObservable(data, "!Foo");
            var result = await target.Take(1);

            Assert.False((bool)result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_For_String_Not_Convertible_To_Boolean()
        {
            var data = new { Foo = "foo" };
            var target = BuildAsObservable(data, "!Foo");
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new BindingChainException($"Unable to convert 'foo' to bool.", "!Foo", "!"),
                    BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_For_Value_Not_Convertible_To_Boolean()
        {
            var data = new { Foo = new object() };
            var target = BuildAsObservable(data, "!Foo");
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new BindingChainException("Unable to convert 'System.Object' to bool.", "!Foo", "!"),
                    BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_BindingNotification_Value()
        {
            var data = new { Foo = true };
            var target = BuildAsObservable(data, "!Foo", enableDataValidation: true);
            var result = await target.Take(1);

            Assert.Equal(new BindingNotification(false), result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Pass_Through_BindingNotification_Error()
        {
            var data = new object();
            var target = BuildAsObservable(data, "!Foo", enableDataValidation: true);
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new BindingChainException("Could not find a matching property accessor for 'Foo' on 'System.Object'.", "!Foo", "Foo"),
                    BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_BindingNotification_Error_FallbackValue()
        {
            var data = new Test { DataValidationError = "Test error" };
            var target = BuildAsObservable(data, "!Foo", enableDataValidation: true);
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new DataValidationException("Test error"),
                    BindingErrorType.DataValidationError,
                    true),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Return_False_For_Invalid_Value()
        {
            var data = new { Foo = "foo" };
            var target = Build(data, "!Foo");
            target.ToObservable().Subscribe(_ => { });

            Assert.False(target.WriteValueToSource("bar"));

            GC.KeepAlive(data);
        }

        private static BindingExpression Build(object source, string path, bool enableDataValidation = false)
        {
            var r = new CharacterReader(path);
            var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
            var nodes = ExpressionNodeFactory.CreateFromAst(grammar, null, null, out _);
            return new BindingExpression(
                source, 
                nodes,
                AvaloniaProperty.UnsetValue,
                enableDataValidation: enableDataValidation);
        }

        private static IObservable<object> BuildAsObservable(object source, string path, bool enableDataValidation = false)
        {
            return Build(source, path, enableDataValidation).ToObservable();
        }

        private class Test : INotifyDataErrorInfo
        {
            private string _dataValidationError;

            public bool Foo { get; set; }
            public object Bar { get; set; }

            public string DataValidationError
            {
                get => _dataValidationError;
                set
                {
                    if (value == _dataValidationError)
                        return;
                    _dataValidationError = value;
                    ErrorsChanged?
                        .Invoke(this, new DataErrorsChangedEventArgs(nameof(DataValidationError)));
                }
            }
            public bool HasErrors => !string.IsNullOrWhiteSpace(DataValidationError);

            public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

            public IEnumerable GetErrors(string propertyName)
            {
                return DataValidationError is object ? new[] { DataValidationError } : null;
            }
        }
    }
}
