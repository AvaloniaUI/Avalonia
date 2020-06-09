using System.ComponentModel;
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
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
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
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
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
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");
                var vm = new ViewModel();

                textBlock.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(textBlock.Text);
            }
        }


        [Theory]
        [InlineData(null, "Not called")]
        [InlineData("A", "Do A")]
        public void Binding_Method_With_Parameter_To_Command_CanExecute(object commandParameter, string result)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button' Command='{Binding Do}' CommandParameter='{Binding Parameter, Mode=OneTime}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.FindControl<Button>("button");
                var vm = new ViewModel()
                {
                    Parameter = commandParameter
                };

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);
                PerformClick(button);
                Assert.Equal(vm.Value, result);
            }
        }

        [Fact]
        public void Binding_Method_With_Parameter_To_Command_CanExecute_DependsOn()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Button Name='button' Command='{Binding Do}' CommandParameter='{Binding Parameter, Mode=OneWay}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.FindControl<Button>("button");
                var vm = new ViewModel()
                {
                    Parameter = null,
                };

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);

                Assert.Equal(button.IsEffectivelyEnabled, false);

                vm.Parameter = true;
                Threading.Dispatcher.UIThread.RunJobs();

                Assert.Equal(button.IsEffectivelyEnabled, true);
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

        private class ViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public string Method() => Value = "Called";
            public string Method1(int i) => Value = $"Called {i}";
            public string Method2(int i, int j) => Value = $"Called {i},{j}";
            public string Value { get; private set; } = "Not called";

            object _parameter;
            public object Parameter
            {
                get
                {
                    return _parameter;
                }
                set
                {
                    if (_parameter == value)
                    {
                        return;
                    }
                    _parameter = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parameter)));
                }
            }

            public void Do(object parameter)
            {
                Value = $"Do {parameter}";
            }

            [Metadata.DependsOn(nameof(Parameter))]
            public bool CanDo(object parameter)
            {
                return ReferenceEquals(null, parameter) == false;
            }
        }
    }
}
