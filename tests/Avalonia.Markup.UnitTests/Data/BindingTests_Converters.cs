using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Media;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_Converters : ScopedTestBase
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

            var expression = (BindingExpression)target.CreateInstance(
                textBlock,
                TextBlock.TextProperty,
                null);

            Assert.Same(StringConverters.IsNullOrEmpty, expression.Converter);
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

        [Fact]
        public void Converter_UnsetValue_Should_Reveal_Lower_Priority_Value()
        {
            var source = new BrushSource(-1);
            var styleBrush = Brushes.White;
            var localBrush = Brushes.Red;
            var textBlock = new TextBlock
            {
                DataContext = source,
            };

            textBlock.SetValue(TextBlock.ForegroundProperty, styleBrush, BindingPriority.Style);
            textBlock.Bind(
                TextBlock.ForegroundProperty,
                new Binding(nameof(BrushSource.Value))
                {
                    Converter = new IntToBrushConverter(localBrush),
                });

            Assert.Same(styleBrush, textBlock.Foreground);

            source.Value = 1;
            Assert.Same(localBrush, textBlock.Foreground);

            source.Value = -1;
            Assert.Same(styleBrush, textBlock.Foreground);

            source.Value = 2;
            Assert.Same(localBrush, textBlock.Foreground);
        }

        private class Class1
        {
            public string Foo { get; set; } = "foo";
        }

        private class BrushSource(int value) : INotifyPropertyChanged
        {
            private int _value = value;

            public event PropertyChangedEventHandler? PropertyChanged;

            public int Value
            {
                get => _value;
                set
                {
                    _value = value;
                    RaisePropertyChanged();
                }
            }

            private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private class IntToBrushConverter(IBrush brush) : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value is int { } i && i >= 0 ? brush : AvaloniaProperty.UnsetValue;
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }
    }
}
