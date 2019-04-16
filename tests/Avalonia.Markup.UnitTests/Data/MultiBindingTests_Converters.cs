// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See license.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class MultiBindingTests_Converters
    {
        [Fact]
        public void StringFormat_Should_Be_Applied()
        {
            var textBlock = new TextBlock
            {
                DataContext = new MultiBindingTests_Converters.Class1(),
            };

            var target = new MultiBinding
            {
                StringFormat = "Foo + Bar = {0}",
                Converter = new SumOfDoublesConverter(),
                Bindings =
                {
                    new Binding(nameof(MultiBindingTests_Converters.Class1.Foo)),
                    new Binding(nameof(MultiBindingTests_Converters.Class1.Bar)),
                }
            };

            textBlock.Bind(TextBlock.TextProperty, target);

            Assert.Equal("Foo + Bar = 3", textBlock.Text);
        }

        [Fact]
        public void StringFormat_Should_Not_Be_Applied_When_Binding_To_Non_String_Or_Object()
        {
            var textBlock = new TextBlock
            {
                DataContext = new MultiBindingTests_Converters.Class1(),
            };
            
            var target = new MultiBinding
            {
                StringFormat = "Hello {0}",
                Converter = new SumOfDoublesConverter(),
                Bindings =
                {
                    new Binding(nameof(MultiBindingTests_Converters.Class1.Foo)),
                    new Binding(nameof(MultiBindingTests_Converters.Class1.Bar)),
                }
            };

            textBlock.Bind(TextBlock.WidthProperty, target);
            
            Assert.Equal(3.0, textBlock.Width);
        }

        private class SumOfDoublesConverter : IMultiValueConverter
        {
            public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
            {
                return values.OfType<double>().Sum();
            }
        }

        private class Class1
        {
            public double Foo { get; set; } = 1;
            public double Bar { get; set; } = 2;
        }
    }
}
