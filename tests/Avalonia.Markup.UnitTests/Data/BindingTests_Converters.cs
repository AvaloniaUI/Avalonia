using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Moq;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_Converters
    {
        [Fact]
        public void Converter_Should_Be_Used()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new Binding(nameof(Class1.Foo))
            {
                Converter = StringConverters.IsNullOrEmpty,
            };

            var expressionObserver = (BindingExpression)target.Initiate(
                textBlock,
                TextBlock.TextProperty).Expression;

            Assert.Same(StringConverters.IsNullOrEmpty, expressionObserver.Converter);
        }

        [Fact]
        public void StringFormat_Should_Be_Applied()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new Binding(nameof(Class1.Foo))
            {
                StringFormat = "Hello {0}",
            };

            textBlock.Bind(TextBlock.TextProperty, target);

            Assert.Equal("Hello foo", textBlock.Text);
        }

        [Fact]
        public void StringFormat_Should_Be_Applied_After_Converter()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new Binding(nameof(Class1.Foo))
            {
                Converter = StringConverters.IsNotNullOrEmpty,
                StringFormat = "Hello {0}",
            };

            textBlock.Bind(TextBlock.TextProperty, target);

            Assert.Equal("Hello True", textBlock.Text);
        }

        [Fact]
        public void ConverterCulture_Should_Be_Passed_To_Converter_Convert()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var culture = new CultureInfo("ar-SA");
            var converter = new Mock<IValueConverter>();
            var target = new Binding(nameof(Class1.Foo))
            {
                Converter = converter.Object,
                ConverterCulture = culture,
            };

            textBlock.Bind(TextBlock.TextProperty, target);

            converter.Verify(converter => converter.Convert(
                "foo",
                typeof(string),
                null,
                culture), 
                Times.Once);
        }

        [Fact]
        public void ConverterCulture_Should_Be_Passed_To_Converter_ConvertBack()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var culture = new CultureInfo("ar-SA");
            var converter = new Mock<IValueConverter>();
            var target = new Binding(nameof(Class1.Foo))
            {
                Converter = converter.Object,
                ConverterCulture = culture,
                Mode = BindingMode.TwoWay,
            };

            textBlock.Bind(TextBlock.TextProperty, target);
            textBlock.Text = "bar";

            converter.Verify(converter => converter.ConvertBack(
                "bar",
                typeof(string),
                null,
                culture),
                Times.Once);
        }

        private class Class1
        {
            public string Foo { get; set; } = "foo";
        }
    }
}
