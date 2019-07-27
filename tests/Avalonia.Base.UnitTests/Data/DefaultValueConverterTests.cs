// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Xunit;
using System.Windows.Input;
using System;
using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace Avalonia.Base.UnitTests.Data.Converters
{
    public class DefaultValueConverterTests
    {
        [Fact]
        public void Can_Convert_String_To_Int()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "5", 
                typeof(int), 
                null, 
                CultureInfo.InvariantCulture);

            Assert.Equal(5, result);
        }

        [Fact]
        public void Can_Convert_String_To_Double()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "5",
                typeof(double),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(5.0, result);
        }

        [Fact]
        public void Can_Convert_String_To_Enum()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "Bar",
                typeof(TestEnum),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(TestEnum.Bar, result);
        }

        [Fact]
        public void Can_Convert_Int_To_Enum()
        {
            var result = DefaultValueConverter.Instance.Convert(
                1,
                typeof(TestEnum),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(TestEnum.Bar, result);
        }

        [Fact]
        public void Can_Convert_Double_To_String()
        {
            var result = DefaultValueConverter.Instance.Convert(
                5.0,
                typeof(string),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal("5", result);
        }

        [Fact]
        public void Can_Convert_Enum_To_Int()
        {
            var result = DefaultValueConverter.Instance.Convert(
                TestEnum.Bar,
                typeof(int),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(1, result);
        }

        [Fact]
        public void Can_Convert_Enum_To_String()
        {
            var result = DefaultValueConverter.Instance.Convert(
                TestEnum.Bar,
                typeof(string),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal("Bar", result);
        }

        [Fact]
        public void Can_Use_Explicit_Cast()
        {
            var result = DefaultValueConverter.Instance.Convert(
                new ExplicitDouble(5.0),
                typeof(double),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(5.0, result);
        }

        [Fact]
        public void Cannot_Convert_Between_Different_Enum_Types()
        {
            var result = DefaultValueConverter.Instance.Convert(
                TestEnum.Foo,
                typeof(Orientation),
                null,
                CultureInfo.InvariantCulture);

            Assert.IsType<BindingNotification>(result);
        }

        [Fact]
        public void Can_Convert_From_Delegate_To_Command()
        {
            int commandResult = 0;

            var result = DefaultValueConverter.Instance.Convert(
                (Action<int>)((int i) => { commandResult = i; }),
                typeof(ICommand),
                null,
                CultureInfo.InvariantCulture);

            Assert.IsAssignableFrom<ICommand>(result);

            (result as ICommand).Execute(5);

            Assert.Equal(5, commandResult);
        }

        [Fact]
        public void Can_Convert_From_Delegate_To_Command_No_Parameters()
        {
            int commandResult = 0;

            var result = DefaultValueConverter.Instance.Convert(
                (Action)(() => { commandResult = 1; }),
                typeof(ICommand),
                null,
                CultureInfo.InvariantCulture);

            Assert.IsAssignableFrom<ICommand>(result);

            (result as ICommand).Execute(null);

            Assert.Equal(1, commandResult);
        }

        private enum TestEnum
        {
            Foo,
            Bar,
        }

        private class ExplicitDouble
        {
            public ExplicitDouble(double value)
            {
                Value = value;
            }

            public double Value { get; }

            public static explicit operator double (ExplicitDouble v)
            {
                return v.Value;
            }
        }
    }
}
