// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class BasicTests
    {
        [Fact]
        public void Simple_Property_Is_Set()
        {
            var xaml = @"<ContentControl xmlns='https://github.com/avaloniaui' Content='Foo'/>";

            var target = (ContentControl)new AvaloniaXamlLoader().Load(xaml);

            Assert.NotNull(target);
            Assert.Equal("Foo", target.Content);
        }

        [Fact]
        public void Default_Content_Property_Is_Set()
        {
            var xaml = @"<ContentControl xmlns='https://github.com/avaloniaui'>Foo</ContentControl>";
            var loader = new AvaloniaXamlLoader();
            var target = (ContentControl)loader.Load(xaml);

            Assert.NotNull(target);
            Assert.Equal("Foo", target.Content);
        }

        [Fact]
        public void ContentControl_ContentTemplate_Is_Functional()
        {
            var xaml =
@"<ContentControl xmlns='https://github.com/avaloniaui'>
    <ContentControl.ContentTemplate>
        <DataTemplate>
            <TextBlock Text='Foo' />
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>";

            var loader = new AvaloniaXamlLoader();
            var contentControl = (ContentControl)loader.Load(xaml);
            var target = contentControl.ContentTemplate;

            Assert.NotNull(target);

            var txt = (TextBlock)target.Build(null);

            Assert.Equal("Foo", txt.Text);
        }

        [Fact]
        public void Named_Control_Is_Added_To_NameScope_Simple()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'>
    <Button Name='button'>Foo</Button>
</UserControl>";
            var loader = new AvaloniaXamlLoader();
            var control = (UserControl)loader.Load(xaml);
            var button = control.FindControl<Button>("button");

            Assert.Equal("Foo", button.Content);
        }

        [Fact]
        public void Named_x_Control_Is_Added_To_NameScope_Simple()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button x:Name='button'>Foo</Button>
</UserControl>";
            var loader = new AvaloniaXamlLoader();
            var control = (UserControl)loader.Load(xaml);
            var button = control.FindControl<Button>("button");

            Assert.Equal("Foo", button.Content);
        }

        [Fact]
        public void Standart_TypeConverter_Is_Used()
        {
            var xaml = @"<UserControl xmlns='https://github.com/avaloniaui' Width='200.5' />";

            var control = (UserControl)new AvaloniaXamlLoader().Load(xaml);
            Assert.Equal(200.5, control.Width);
        }

        [Fact]
        public void Avalonia_TypeConverter_Is_Used()
        {
            var xaml = @"<UserControl xmlns='https://github.com/avaloniaui' Background='White' />";

            var control = (UserControl)new AvaloniaXamlLoader().Load(xaml);
            var bk = control.Background;
            Assert.IsType<SolidColorBrush>(bk);
            Assert.Equal(Colors.White, (bk as SolidColorBrush).Color);
        }

        [Fact]
        public void Simple_Style_Is_Parsed()
        {
            var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style Selector='TextBlock'>
        <Setter Property='Background' Value='White'/>
        <Setter Property='Width' Value='100'/>
    </Style>
</Styles>";

            var styles = (Styles)new AvaloniaXamlLoader().Load(xaml);

            Assert.Equal(1, styles.Count);

            var style = (Style)styles[0];

            var setters = style.Setters.Cast<Setter>().ToArray();

            Assert.Equal(2, setters.Length);

            Assert.Equal(TextBlock.BackgroundProperty, setters[0].Property);
            Assert.Equal(Brushes.White, setters[0].Value);

            Assert.Equal(TextBlock.WidthProperty, setters[1].Property);
            Assert.Equal(100.0, setters[1].Value);
        }

        [Fact]
        public void Named_Control_Is_Added_To_NameScope()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button Name='button'>Foo</Button>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                Assert.Equal("Foo", button.Content);
            }
        }

        [Fact]
        public void Control_Is_Added_To_Parent_Before_Properties_Are_Set()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <local:InitializationOrderTracker Width='100'/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var tracker = (InitializationOrderTracker)window.Content;

                var attached = tracker.Order.IndexOf("AttachedToLogicalTree");
                var widthChanged = tracker.Order.IndexOf("Property Width Changed");

                Assert.NotEqual(-1, attached);
                Assert.NotEqual(-1, widthChanged);
                Assert.True(attached < widthChanged);
            }
        }
    }
}