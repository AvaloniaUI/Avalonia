// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reactive.Linq;
using Moq;
using Perspex.Markup.Data;
using Xunit;

namespace Perspex.Markup.UnitTests.Data
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
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var data = new Class1 { StringValue = "5.6" };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(double));
            var result = await target.Take(1);

            Assert.Equal(5.6, result);
        }

        [Fact]
        public async void Should_Convert_Get_Invalid_Double_String_To_UnsetValue()
        {
            var data = new Class1 { StringValue = "foo" };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(double));
            var result = await target.Take(1);

            Assert.Equal(PerspexProperty.UnsetValue, result);
        }

        [Fact]
        public async void Should_Coerce_Get_Null_Double_String_To_UnsetValue()
        {
            var data = new Class1 { StringValue = null };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(double));
            var result = await target.Take(1);

            Assert.Equal(PerspexProperty.UnsetValue, result);
        }

        [Fact]
        public void Should_Convert_Set_String_To_Double()
        {
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var data = new Class1 { StringValue = "5.6" };
            var target = new ExpressionSubject(new ExpressionObserver(data, "StringValue"), typeof(double));

            target.OnNext(6.7);

            Assert.Equal("6.7", data.StringValue);
        }

        [Fact]
        public async void Should_Convert_Get_Double_To_String()
        {
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var data = new Class1 { DoubleValue = 5.6 };
            var target = new ExpressionSubject(new ExpressionObserver(data, "DoubleValue"), typeof(string));
            var result = await target.Take(1);

            Assert.Equal("5.6", result);
        }

        [Fact]
        public void Should_Convert_Set_Double_To_String()
        {
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var data = new Class1 { DoubleValue = 5.6 };
            var target = new ExpressionSubject(new ExpressionObserver(data, "DoubleValue"), typeof(string));

            target.OnNext("6.7");

            Assert.Equal(6.7, data.DoubleValue);
        }

        [Fact]
        public void Should_Coerce_Set_Invalid_Double_String_To_Default_Value()
        {
            var data = new Class1 { DoubleValue = 5.6 };
            var target = new ExpressionSubject(new ExpressionObserver(data, "DoubleValue"), typeof(string));

            target.OnNext("foo");

            Assert.Equal(0, data.DoubleValue);
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

            target.OnNext(PerspexProperty.UnsetValue);

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
                "foo");

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
                "foo");

            target.OnNext("bar");

            converter.Verify(x => x.ConvertBack("bar", typeof(double), "foo", CultureInfo.CurrentUICulture));
        }

        private class Class1 : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public string StringValue { get; set; }

            public double DoubleValue { get; set; }

            public void RaisePropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
