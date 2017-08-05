// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class BindingTests_RelativeSource
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
    <Button Name='button' Content='{Binding Foo, RelativeSource={RelativeSource DataContext}}'/>
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
        public void Binding_To_Self_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button' Content='{Binding Name, RelativeSource={RelativeSource Self}}'/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();

                Assert.Equal("button", button.Content);
            }
        }

        [Fact]
        public void Binding_To_First_Ancestor_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding Name, RelativeSource={RelativeSource AncestorType=Border}}'/>
      </Border>
    </Border>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();

                Assert.Equal("border2", button.Content);
            }
        }

        [Fact]
        public void Binding_To_Second_Ancestor_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding Name, RelativeSource={RelativeSource AncestorType=Border, AncestorLevel=2}}'/>
      </Border>
    </Border>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();

                Assert.Equal("border1", button.Content);
            }
        }

        [Fact]
        public void Binding_To_Ancestor_With_Namespace_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<local:TestWindow xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
        Title='title'>
  <Button Name='button' Content='{Binding Title, RelativeSource={RelativeSource AncestorType=local:TestWindow}}'/>
</local:TestWindow>";
                var loader = new AvaloniaXamlLoader();
                var window = (TestWindow)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();

                Assert.Equal("title", button.Content);
            }
        }
    }

    public class TestWindow : Window { }
}