// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.UnitTests;
using System;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class BindingTests_RelativeSource : XamlTestBase
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
        public void Binding_To_First_Ancestor_Without_AncestorType_Throws_Exception()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border1'>
      <ContentControl Name='contentControl'>
        <Button Name='button' Content='{Binding Name, RelativeSource={RelativeSource AncestorLevel=1}}'/>
      </ContentControl>
    </Border>
</Window>";
                var loader = new AvaloniaXamlLoader();
                Assert.Throws<InvalidOperationException>( () => loader.Load(xaml));
            }
        }

        [Fact]
        public void Binding_To_First_Ancestor_With_Shorthand_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding $parent.Name}'/>
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
        public void Binding_To_First_Ancestor_With_Shorthand_Uses_LogicalTree()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border'>
      <ContentControl Name='contentControl'>
        <Button Name='button' Content='{Binding $parent.Name}'/>
      </ContentControl>
    </Border>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var contentControl = window.FindControl<ContentControl>("contentControl");
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();
                
                Assert.Equal("contentControl", button.Content);
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
        public void Binding_To_Second_Ancestor_With_Shorthand_Uses_LogicalTree()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <ContentControl Name='contentControl1'>
      <ContentControl Name='contentControl2'>
        <Button Name='button' Content='{Binding $parent[1].Name}'/>
      </ContentControl>
    </ContentControl>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var contentControl1 = window.FindControl<ContentControl>("contentControl1");
                var contentControl2 = window.FindControl<ContentControl>("contentControl2");
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();

                Assert.Equal("contentControl1", button.Content);
            }
        }

        [Fact]
        public void Binding_To_Ancestor_Of_Type_With_Shorthand_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding $parent[Border].Name}'/>
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
        public void Binding_To_Second_Ancestor_With_Shorthand_And_Type_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding $parent[Border; 1].Name}'/>
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
        public void Binding_To_Second_Ancestor_With_Shorthand_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding $parent[1].Name}'/>
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

        [Fact]
        public void Shorthand_Binding_With_Negation_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding !$self.IsDefault}'/>
      </Border>
    </Border>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();

#pragma warning disable xUnit2004 // Diagnostic mis-firing since button.Content isn't guaranteed to be a bool.
                Assert.Equal(true, button.Content);
#pragma warning restore xUnit2004
            }
        }
        [Fact]
        public void Shorthand_Binding_With_Multiple_Negation_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding !!$self.IsDefault}'/>
      </Border>
    </Border>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                window.ApplyTemplate();

#pragma warning disable xUnit2004 // Diagnostic mis-firing since button.Content isn't guaranteed to be a bool.
                Assert.Equal(false, button.Content);
#pragma warning restore xUnit2004
            }
        }
    }

    public class TestWindow : Window { }
}
