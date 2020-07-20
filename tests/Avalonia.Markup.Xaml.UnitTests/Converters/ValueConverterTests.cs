using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Converters
{
    public class ValueConverterTests : XamlTestBase
    {
        [Fact]
        public void ValueConverter_Special_Values_Work()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:c='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Converters;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <TextBlock Name='textBlock' Text='{Binding Converter={x:Static c:TestConverter.Instance}, FallbackValue=bar}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.ApplyTemplate();

                window.DataContext = 2;
                Assert.Equal("foo", textBlock.Text);

                window.DataContext = -3;
                Assert.Equal("foo", textBlock.Text);

                window.DataContext = 0;
                Assert.Equal("bar", textBlock.Text);
            }
        }
    }

    public class TestConverter : IValueConverter
    {
        public static readonly TestConverter Instance = new TestConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
            {
                if (i > 0)
                {
                    return "foo";
                }

                if (i == 0)
                {
                    return AvaloniaProperty.UnsetValue;
                }

                return BindingOperations.DoNothing;
            }

            return "(default)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
