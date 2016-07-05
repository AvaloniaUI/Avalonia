// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reactive.Linq;
using Moq;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Xunit;
using System.Threading;

namespace Avalonia.Markup.UnitTests.Data
{
    public class ExpressionSubjectTests
    {
        [Fact]
        public async void Should_Get_Simple_Property_Value()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(string));
            var result = await target.Take(1);

            Assert.Equal("foo", result);
        }

        [Fact]
        public void Should_Set_Simple_Property_Value()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(string));

            target.OnNext("bar");

            Assert.Equal("bar", data.StringValue);
        }

        [Fact]
        public async void Should_Convert_Get_String_To_Double()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var data = new Class1 { StringValue = "5.6" };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(double));
            var result = await target.Take(1);

            Assert.Equal(5.6, result);
        }

        [Fact]
        public async void Getting_Invalid_Double_String_Should_Return_BindingError()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(double));
            var result = await target.Take(1);

            Assert.IsType<BindingError>(result);
        }

        [Fact]
        public async void Should_Coerce_Get_Null_Double_String_To_UnsetValue()
        {
            var data = new Class1 { StringValue = null };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(double));
            var result = await target.Take(1);

            Assert.Equal(AvaloniaProperty.UnsetValue, result);
        }

        [Fact]
        public void Should_Convert_Set_String_To_Double()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var data = new Class1 { StringValue = (5.6).ToString() };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(double));

            target.OnNext(6.7);

            Assert.Equal((6.7).ToString(), data.StringValue);
        }

        [Fact]
        public async void Should_Convert_Get_Double_To_String()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var data = new Class1 { DoubleValue = 5.6 };
            var target = new ExpressionSubject(new ExpressionObserver(data, "DoubleValue"), typeof(string));
            var result = await target.Take(1);

            Assert.Equal((5.6).ToString(), result);
        }

        [Fact]
        public void Should_Convert_Set_Double_To_String()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var data = new Class1 { DoubleValue = 5.6 };
            var target = new ExpressionSubject(new ExpressionObserver(data, "DoubleValue"), typeof(string));

            target.OnNext("6.7");

            Assert.Equal(6.7, data.DoubleValue);
        }

        [Fact]
        public void Setting_Invalid_Double_String_Should_Not_Change_Target()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new ExpressionSubject(new ExpressionObserver(data, "DoubleValue"), typeof(string));

            target.OnNext("foo");

            Assert.Equal(5.6, data.DoubleValue);
        }

        [Fact]
        public void Setting_Invalid_Double_String_Should_Use_FallbackValue()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new ExpressionSubject(
                new ExpressionObserver(data, "DoubleValue"),
                typeof(string),
                "9.8",
                DefaultValueConverter.Instance);

            target.OnNext("foo");

            Assert.Equal(9.8, data.DoubleValue);
        }

        [Fact]
        public void Should_Coerce_Setting_Null_Double_To_Default_Value()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new ExpressionSubject(new ExpressionObserver(data, "DoubleValue"), typeof(string));

            target.OnNext(null);

            Assert.Equal(0, data.DoubleValue);
        }

        [Fact]
        public void Should_Coerce_Setting_UnsetValue_Double_To_Default_Value()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new ExpressionSubject(new ExpressionObserver(data, "DoubleValue"), typeof(string));

            target.OnNext(AvaloniaProperty.UnsetValue);

            Assert.Equal(0, data.DoubleValue);
        }

        [Fact]
        public void Should_Pass_ConverterParameter_To_Convert()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var converter = new Mock<IValueConverter>();
            var target = new ExpressionSubject(
                new ExpressionObserver(data, "DoubleValue"),
                typeof(string),
                converter.Object,
                converterParameter: "foo");

            target.Subscribe(_ => { });

            converter.Verify(x => x.Convert(5.6, typeof(string), "foo", CultureInfo.CurrentUICulture));
        }

        [Fact]
        public void Should_Pass_ConverterParameter_To_ConvertBack()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var converter = new Mock<IValueConverter>();
            var target = new ExpressionSubject(
                new ExpressionObserver(data, "DoubleValue"), 
                typeof(string),
                converter.Object,
                converterParameter: "foo");

            target.OnNext("bar");

            converter.Verify(x => x.ConvertBack("bar", typeof(double), "foo", CultureInfo.CurrentUICulture));
        }

        private class Class1 : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public string StringValue { get; set; }

            public double DoubleValue { get; set; }
        }
    }
}
