using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Converters
{
    public class MultiValueConverterTests : XamlTestBase
    {
        [Fact]
        public void MultiValueConverter_Special_Values_Work()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:c='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Converters;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <TextBlock Name='textBlock'>
        <TextBlock.Text>
            <MultiBinding Converter='{x:Static c:TestMultiValueConverter.Instance}' FallbackValue='bar'>
                <Binding Path='Item1' />
                <Binding Path='Item2' />
            </MultiBinding>
        </TextBlock.Text>
    </TextBlock>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.ApplyTemplate();

                window.DataContext = Tuple.Create(2, 2);
                Assert.Equal("foo", textBlock.Text);

                window.DataContext = Tuple.Create(-3, 3);
                Assert.Equal("foo", textBlock.Text);

                window.DataContext = Tuple.Create(0, 2);
                Assert.Equal("bar", textBlock.Text);
            }
        }
    }

    public class TestMultiValueConverter : IMultiValueConverter
    {
        public static readonly TestMultiValueConverter Instance = new TestMultiValueConverter();

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is int i && values[1] is int j)
            {
                var p = i * j;

                if (p > 0)
                {
                    return "foo";
                }

                if (p == 0)
                {
                    return AvaloniaProperty.UnsetValue;
                }

                return BindingOperations.DoNothing;
            }

            return "(default)";
        }
    }
}
