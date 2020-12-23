using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests : XamlTestBase
    {
        [Fact]
        public void Binding_With_Null_Path_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textBlock' Text='{Binding}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.DataContext = "foo";
                window.ApplyTemplate();

                Assert.Equal("foo", textBlock.Text);
            }
        }

        [Fact]
        public void Binding_To_DoNothing_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textBlock' Text='{Binding}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.ApplyTemplate();

                window.DataContext = "foo";
                Assert.Equal("foo", textBlock.Text);

                window.DataContext = BindingOperations.DoNothing;
                Assert.Equal("foo", textBlock.Text);

                window.DataContext = "bar";
                Assert.Equal("bar", textBlock.Text);
            }
        }

        [Fact]
        public void MultiBinding_TemplatedParent_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Data;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <TextBox Name='textBox' Text='Foo' Watermark='Bar'>
        <TextBox.Template>
            <ControlTemplate>
                <TextPresenter Name='PART_TextPresenter'>
                    <TextPresenter.Text>
                        <MultiBinding Converter='{x:Static local:ConcatConverter.Instance}'>
                            <Binding RelativeSource='{RelativeSource TemplatedParent}' Path='Text'/>
                            <Binding RelativeSource='{RelativeSource TemplatedParent}' Path='Watermark'/>
                        </MultiBinding>
                    </TextPresenter.Text>
                </TextPresenter>
            </ControlTemplate>
        </TextBox.Template>
    </TextBox>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBox = window.FindControl<TextBox>("textBox");

                window.ApplyTemplate();
                textBox.ApplyTemplate();

                var target = (TextPresenter)textBox.GetVisualChildren().Single();
                Assert.Equal("Foo,Bar", target.Text);
            }
        }
    }

    public class ConcatConverter : IMultiValueConverter
    {
        public static ConcatConverter Instance { get; } = new ConcatConverter();

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Join(",", values);
        }
    }
}
