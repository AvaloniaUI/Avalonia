// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class BasicTests
    {
        [Fact]
        public void Simple_Property_Is_Set()
        {

            var xaml = @"
<ContentControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             Content='Foo'>
</ContentControl>";
            var loader = new AvaloniaXamlLoader();
            var target = (ContentControl)loader.Load(xaml);

            Assert.NotNull(target);
            Assert.Equal("Foo", target.Content);
        }

        [Fact]
        public void Default_Content_Property_Is_Set()
        {
            var xaml = @"
<ContentControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
>Foo</ContentControl>";
            var loader = new AvaloniaXamlLoader();
            var target = (ContentControl)loader.Load(xaml);

            Assert.NotNull(target);
            Assert.Equal("Foo", target.Content);
        }

        [Fact]
        public void ContentControl_ContentTemplate_Is_Functional()
        {
            var xaml =
@"<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
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
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
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
