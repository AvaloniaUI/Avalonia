// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class BindingTests
    {
        [Fact]
        public void Binding_To_DataContext_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button' Content='{Binding Foo}'/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                button.DataContext = new { Foo = "foo" };
                window.ApplyTemplate();

                Assert.Equal("foo", button.Content);
            }
        }

        [Fact]
        public void Longhand_Binding_To_DataContext_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button'>
        <Button.Content>
            <Binding Path='Foo'/>
        </Button.Content>
    </Button>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                button.DataContext = new { Foo = "foo" };
                window.ApplyTemplate();

                Assert.Equal("foo", button.Content);
            }
        }

        [Fact]
        public void Can_Bind_Control_To_Non_Control()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button' Content='Foo'>
        <Button.Tag>
            <local:NonControl Control='{Binding #button}'/>
        </Button.Tag>
    </Button>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                Assert.Same(button, ((NonControl)button.Tag).Control);
            }
        }

        [Fact]
        public void Can_Bind_To_DataContext_Of_Anchor_On_Non_Control()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button'>
        <Button.Tag>
            <local:NonControl String='{Binding Foo}'/>
        </Button.Tag>
    </Button>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                button.DataContext = new { Foo = "foo" };

                Assert.Equal("foo", ((NonControl)button.Tag).String);
            }
        }

        [Fact]
        public void Binding_To_Window_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Title='{Binding Foo}'>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);

                window.DataContext = new { Foo = "foo" };
                window.ApplyTemplate();

                Assert.Equal("foo", window.Title);
            }
        }

        [Fact]
        public void Binding_DataContext_To_Inherited_DataContext_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border DataContext='{Binding Foo}'/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var border = (Border)window.Content;

                window.DataContext = new { Foo = "foo" };
                window.ApplyTemplate();

                Assert.Equal("foo", border.DataContext);
            }
        }


        [Fact]
        public void Binding_To_Self_Works()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textblock' Text='{Binding Tag, RelativeSource={RelativeSource Self}}'/>
</Window>";

                var window = AvaloniaXamlLoader.Parse<ContentControl>(xaml);
                var textBlock = (TextBlock)window.Content;

                textBlock.Tag = "foo";

                Assert.Equal("foo", textBlock.Text);
            }
        }

        [Fact]
        public void Longform_Binding_To_Self_Works()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textblock' Tag='foo'>
        <TextBlock.Text>
            <Binding RelativeSource='{RelativeSource Self}' Path='Tag'/>
        </TextBlock.Text>
    </TextBlock>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var textBlock = (TextBlock)window.Content;

                window.ApplyTemplate();

                Assert.Equal("foo", textBlock.Text);
            }
        }

        [Fact]
        public void Stream_Binding_To_Observable_Works()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textblock' Text='{Binding Observable^}'/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var textBlock = (TextBlock)window.Content;
                var observable = new BehaviorSubject<string>("foo");

                window.DataContext = new { Observable = observable };
                window.ApplyTemplate();

                Assert.Equal("foo", textBlock.Text);
                observable.OnNext("bar");
                Assert.Equal("bar", textBlock.Text);
            }
        }
    }
}