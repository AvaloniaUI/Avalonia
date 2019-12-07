// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class BindingExpressionTests : IClassFixture<InvariantCultureFixture>
    {
        [Fact]
        public async Task Should_Get_Simple_Property_Value()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.StringValue), typeof(string));
            var result = await target.Take(1);

            Assert.Equal("foo", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Set_Simple_Property_Value()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.StringValue), typeof(string));

            target.OnNext("bar");

            Assert.Equal("bar", data.StringValue);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Set_Indexed_Value()
        {
            var data = new { Foo = new[] { "foo" } };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.Foo[0]), typeof(string));

            target.OnNext("bar");

            Assert.Equal("bar", data.Foo[0]);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Convert_Get_String_To_Double()
        {
            var data = new Class1 { StringValue = $"{5.6}" };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.StringValue), typeof(double));
            var result = await target.Take(1);

            Assert.Equal(5.6, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Getting_Invalid_Double_String_Should_Return_BindingError()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.StringValue), typeof(double));
            var result = await target.Take(1);

            Assert.IsType<BindingNotification>(result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Coerce_Get_Null_Double_String_To_UnsetValue()
        {
            var data = new Class1 { StringValue = null };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.StringValue), typeof(double));
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Convert_Set_String_To_Double()
        {
            var data = new Class1 { StringValue = $"{5.6}" };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.StringValue), typeof(double));

            target.OnNext(6.7);

            Assert.Equal($"{6.7}", data.StringValue);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Convert_Get_Double_To_String()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.DoubleValue), typeof(string));
            var result = await target.Take(1);

            Assert.Equal($"{5.6}", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Convert_Set_Double_To_String()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.DoubleValue), typeof(string));

            target.OnNext($"{6.7}");

            Assert.Equal(6.7, data.DoubleValue);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_With_FallbackValue_For_NonConvertibe_Target_Value()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new BindingExpression(
                ExpressionObserver.Create(data, o => o.StringValue),
                typeof(int),
                42,
                AvaloniaProperty.UnsetValue,
                DefaultValueConverter.Instance);
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new InvalidCastException("'foo' is not a valid number."),
                    BindingErrorType.Error,
                    42),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_With_FallbackValue_For_NonConvertibe_Target_Value_With_Data_Validation()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new BindingExpression(
                ExpressionObserver.Create(data, o => o.StringValue, true),
                typeof(int),
                42,
                AvaloniaProperty.UnsetValue,
                DefaultValueConverter.Instance);
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new InvalidCastException("'foo' is not a valid number."),
                    BindingErrorType.Error,
                    42),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_For_Invalid_FallbackValue()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new BindingExpression(
                ExpressionObserver.Create(data, o => o.StringValue),
                typeof(int),
                "bar",
                AvaloniaProperty.UnsetValue,
                DefaultValueConverter.Instance);
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new AggregateException(
                        new InvalidCastException("'foo' is not a valid number."),
                        new InvalidCastException("Could not convert FallbackValue 'bar' to 'System.Int32'")),
                    BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_For_Invalid_FallbackValue_With_Data_Validation()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new BindingExpression(
                ExpressionObserver.Create(data, o => o.StringValue, true),
                typeof(int),
                "bar",
                AvaloniaProperty.UnsetValue,
                DefaultValueConverter.Instance);
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                    new AggregateException(
                        new InvalidCastException("'foo' is not a valid number."),
                        new InvalidCastException("Could not convert FallbackValue 'bar' to 'System.Int32'")),
                    BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Setting_Invalid_Double_String_Should_Not_Change_Target()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.DoubleValue), typeof(string));

            target.OnNext("foo");

            Assert.Equal(5.6, data.DoubleValue);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Setting_Invalid_Double_String_Should_Use_FallbackValue()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new BindingExpression(
                ExpressionObserver.Create(data, o => o.DoubleValue),
                typeof(string),
                "9.8",
                AvaloniaProperty.UnsetValue,
                DefaultValueConverter.Instance);

            target.OnNext("foo");

            Assert.Equal(9.8, data.DoubleValue);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Coerce_Setting_Null_Double_To_Default_Value()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.DoubleValue), typeof(string));

            target.OnNext(null);

            Assert.Equal(0, data.DoubleValue);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Coerce_Setting_UnsetValue_Double_To_Default_Value()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.DoubleValue), typeof(string));

            target.OnNext(AvaloniaProperty.UnsetValue);

            Assert.Equal(0, data.DoubleValue);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Pass_ConverterParameter_To_Convert()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var converter = new Mock<IValueConverter>();

            var target = new BindingExpression(
                ExpressionObserver.Create(data, o => o.DoubleValue),
                typeof(string),
                converter.Object,
                converterParameter: "foo");

            target.Subscribe(_ => { });

            converter.Verify(x => x.Convert(5.6, typeof(string), "foo", CultureInfo.CurrentCulture));

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Pass_ConverterParameter_To_ConvertBack()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var converter = new Mock<IValueConverter>();
            var target = new BindingExpression(
                ExpressionObserver.Create(data, o => o.DoubleValue),
                typeof(string),
                converter.Object,
                converterParameter: "foo");

            target.OnNext("bar");

            converter.Verify(x => x.ConvertBack("bar", typeof(double), "foo", CultureInfo.CurrentCulture));

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Handle_DataValidation()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var converter = new Mock<IValueConverter>();
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.DoubleValue, true), typeof(string));
            var result = new List<object>();

            target.Subscribe(x => result.Add(x));
            target.OnNext(1.2);
            target.OnNext($"{3.4}");
            target.OnNext("bar");

            Assert.Equal(
                new[]
                {
                    new BindingNotification($"{5.6}"),
                    new BindingNotification($"{1.2}"),
                    new BindingNotification($"{3.4}"),
                    new BindingNotification(
                        new InvalidCastException("'bar' is not a valid number."),
                        BindingErrorType.Error)
                },
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Second_Subscription_Should_Fire_Immediately()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new BindingExpression(ExpressionObserver.Create(data, o => o.StringValue), typeof(string));
            object result = null;

            target.Subscribe();
            target.Subscribe(x => result = x);

            Assert.Equal("foo", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Null_Value_Should_Use_TargetNullValue()
        {
            var data = new Class1 { StringValue = "foo" };

            var target = new BindingExpression(
                ExpressionObserver.Create(data, o => o.StringValue),
                typeof(string),
                AvaloniaProperty.UnsetValue,
                "bar",
                DefaultValueConverter.Instance);

            object result = null;
            target.Subscribe(x => result = x);

            Assert.Equal("foo", result);
            
            data.StringValue = null;
            Assert.Equal("bar", result);

            GC.KeepAlive(data);
        }

        private class Class1 : NotifyingBase
        {
            private string _stringValue;
            private double _doubleValue;

            public string StringValue
            {
                get { return _stringValue; }
                set { _stringValue = value; RaisePropertyChanged(); }
            }

            public double DoubleValue
            {
                get { return _doubleValue; }
                set { _doubleValue = value; RaisePropertyChanged(); }
            }
        }
    }
}
