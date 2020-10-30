using System;
using System.Collections;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Markup.Parsers;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class ExpressionObserverBuilderTests_Negation
    {
        [Fact]
        public async Task Should_Negate_0()
        {
            var data = new { Foo = 0 };
            var target = ExpressionObserverBuilder.Build(data, "!Foo");
            var result = await target.Take(1);

            Assert.True((bool)result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_1()
        {
            var data = new { Foo = 1 };
            var target = ExpressionObserverBuilder.Build(data, "!Foo");
            var result = await target.Take(1);

            Assert.False((bool)result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_False_String()
        {
            var data = new { Foo = "false" };
            var target = ExpressionObserverBuilder.Build(data, "!Foo");
            var result = await target.Take(1);

            Assert.True((bool)result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_True_String()
        {
            var data = new { Foo = "True" };
            var target = ExpressionObserverBuilder.Build(data, "!Foo");
            var result = await target.Take(1);

            Assert.False((bool)result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_For_String_Not_Convertible_To_Boolean()
        {
            var data = new { Foo = "foo" };
            var target = ExpressionObserverBuilder.Build(data, "!Foo");
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new InvalidCastException($"Unable to convert 'foo' to bool."),
                    BindingErrorType.Error), 
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_For_Value_Not_Convertible_To_Boolean()
        {
            var data = new { Foo = new object() };
            var target = ExpressionObserverBuilder.Build(data, "!Foo");
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new InvalidCastException($"Unable to convert 'System.Object' to bool."),
                    BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_BindingNotification_Value()
        {
            var data = new { Foo = true };
            var target = ExpressionObserverBuilder.Build(data, "!Foo", enableDataValidation: true);
            var result = await target.Take(1);

            Assert.Equal(new BindingNotification(false), result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Pass_Through_BindingNotification_Error()
        {
            var data = new { };
            var target = ExpressionObserverBuilder.Build(data, "!Foo", enableDataValidation: true);
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new MissingMemberException("Could not find a matching property accessor for 'Foo' on '{ }'"),
                    BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Negate_BindingNotification_Error_FallbackValue()
        {
            var data = new Test { DataValidationError = "Test error" };
            var target = ExpressionObserverBuilder.Build(data, "!Foo", enableDataValidation: true);
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
        public async Task Should_Add_Error_To_BindingNotification_For_FallbackValue_Not_Convertible_To_Boolean()
        {
            var data = new Test { Bar = new object(), DataValidationError = "Test error" };
            var target = ExpressionObserverBuilder.Build(data, "!Bar", enableDataValidation: true);
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new AggregateException(
                        new DataValidationException("Test error"),
                        new InvalidCastException($"Unable to convert 'System.Object' to bool.")),
                    BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Return_False_For_Invalid_Value()
        {
            var data = new { Foo = "foo" };
            var target = ExpressionObserverBuilder.Build(data, "!Foo");
            target.Subscribe(_ => { });

            Assert.False(target.SetValue("bar"));

            GC.KeepAlive(data);
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
