using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
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
                DataContext = new Class1(),
            };

            var format = "{0:0.0} + {1:00}";
            var target = new MultiBinding
            {
                StringFormat = format,
                Bindings =
                {
                    new Binding(nameof(Class1.Foo)),
                    new Binding(nameof(Class1.Bar)),
                }
            };

            textBlock.Bind(TextBlock.TextProperty, target);

            Assert.Equal(string.Format(format, 1, 2), textBlock.Text);
        }

        [Fact]
        public void StringFormat_Should_Be_Applied_After_Converter()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new MultiBinding
            {
                StringFormat = "Foo + Bar = {0}",
                Converter = new SumOfDoublesConverter(),
                Bindings =
                {
                    new Binding(nameof(Class1.Foo)),
                    new Binding(nameof(Class1.Bar)),
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
                DataContext = new Class1(),
            };
            
            var target = new MultiBinding
            {
                StringFormat = "Hello {0}",
                Converter = new SumOfDoublesConverter(),
                Bindings =
                {
                    new Binding(nameof(Class1.Foo)),
                    new Binding(nameof(Class1.Bar)),
                }
            };

            textBlock.Bind(Layoutable.WidthProperty, target);
            
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
