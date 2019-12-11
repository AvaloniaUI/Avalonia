// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reactive.Subjects;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_Method : XamlTestBase
    {
        [Fact]
        public void Binding_Method_To_Command_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button' Command='{Binding Method}'/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");
                var vm = new ViewModel();

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);
                PerformClick(button);
                Assert.Equal("Called", vm.Value);
            }
        }

        [Fact]
        public void Binding_Method_With_Parameter_To_Command_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button' Command='{Binding Method1}' CommandParameter='5'/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");
                var vm = new ViewModel();

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);
                PerformClick(button);
                Assert.Equal("Called 5", vm.Value);
            }
        }
        
        [Fact]
        public void Binding_Method_To_TextBlock_Text_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <TextBlock Name='textBlock' Text='{Binding Method}'/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");
                var vm = new ViewModel();

                textBlock.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(textBlock.Text);
            }
        }

        static void PerformClick(Button button)
        {
            button.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Input.Key.Enter,
            });
        }

        private class ViewModel
        {
            public string Method() => Value = "Called";
            public string Method1(int i) => Value = $"Called {i}";
            public string Method2(int i, int j) => Value = $"Called {i},{j}";
            public string Value { get; private set; } = "Not called";
        }
    }
}
