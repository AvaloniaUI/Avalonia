// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.UnitTests;
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
    }
}
