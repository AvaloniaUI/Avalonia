using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Logging;
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
                var button = window.GetControl<Button>("button");
                var vm = new ViewModel();

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);
                PerformClick(button);
                Assert.Equal("Called", vm.Value);
            }
        }

        [Theory]
        [InlineData("ObjectMethod", "<x:String>hello</x:String>", "Called ObjectMethod with hello")]
        [InlineData("StringMethod", "<x:String>hello</x:String>", "Called StringMethod with hello")]
        [InlineData("Int32Method", "<x:Int32>42</x:Int32>", "Called Int32Method with 42")]
        [InlineData("Int32Method", "<x:String>42</x:String>", "Called Int32Method with 42")]
        [InlineData("VirtualObjectMethod", "<x:String>hello</x:String>", "Called VirtualObjectMethod with hello")]
        [InlineData("VirtualStringMethod", "<x:String>hello</x:String>", "Called VirtualStringMethod with hello")]
        [InlineData("VirtualStringMethod", "<x:Null />", "Called VirtualStringMethod with ")]
        [InlineData("VirtualInt32Method", "<x:Int32>42</x:Int32>", "Called VirtualInt32Method with 42")]
        [InlineData("MethodWithNewSlot", "<x:Int32>42</x:Int32>", "Called MethodWithNewSlot with 42")]
        public void Binding_Method_With_Parameter_To_Command_Uses_Single_Parameter_Overload(
            string methodName,
            string xamlParameter,
            string expected)
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var window = (Window)AvaloniaRuntimeXamlLoader.Load(
                $$"""
                  <Window xmlns='https://github.com/avaloniaui'
                          xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                      <Button Name='button' Command='{Binding {{methodName}}}'>
                        <Button.CommandParameter>
                          {{xamlParameter}}
                        </Button.CommandParameter>
                      </Button>
                  </Window>
                  """);
            var button = window.GetControl<Button>("button");
            var vm = new ViewModel();

            button.DataContext = vm;
            window.ApplyTemplate();

            Assert.NotNull(button.Command);
            PerformClick(button);
            Assert.Equal(expected, vm.Value);
        }

        [Fact]
        public void Binding_Method_With_Parameter_To_Command_Prefers_Object_Overload()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var window = (Window)AvaloniaRuntimeXamlLoader.Load(
                """
                <Window xmlns='https://github.com/avaloniaui'
                        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <Button Name='button' Command='{Binding MethodWithOverloads}' CommandParameter='foo' />
                </Window>
                """);
            var button = window.GetControl<Button>("button");
            var vm = new ViewModel();

            button.DataContext = vm;
            window.ApplyTemplate();

            Assert.NotNull(button.Command);
            PerformClick(button);
            Assert.Equal("Called MethodWithOverloads with Object foo", vm.Value);
        }

        [Fact]
        public void Binding_Method_With_Parameter_To_Command_Fails_With_Multiple_Single_Parameter_Overloads_Without_Object()
        {
            AssertBindingFails(
                "MethodWithOverloads2",
                "Unable to resolve method of name 'MethodWithOverloads2' on type " +
                "'Avalonia.Markup.Xaml.UnitTests.Data.BindingTests_Method+ViewModel'. " +
                "Found 2 overloads accepting one parameter: 'System.Int32', 'System.String'. " +
                "Expected either a single overload with one parameter, or an overload accepting System.Object.");
        }

        [Fact]
        public void Binding_Method_With_Parameter_To_Command_Uses_Parameterless_Overload_When_No_Overloads_With_Parameter_Exist()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var window = (Window)AvaloniaRuntimeXamlLoader.Load(
                """
                <Window xmlns='https://github.com/avaloniaui'
                        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <Button Name='button' Command='{Binding MethodWithOverloads3}' CommandParameter='foo' />
                </Window>
                """);
            var button = window.GetControl<Button>("button");
            var vm = new ViewModel();

            button.DataContext = vm;
            window.ApplyTemplate();

            Assert.NotNull(button.Command);
            PerformClick(button);
            Assert.Equal("Called MethodWithOverloads3 without parameter", vm.Value);
        }

        [Fact]
        public void Binding_Method_With_Parameter_To_Command_Fails_Without_Valid_Overloads()
        {
            AssertBindingFails(
                "MethodWithOverloads4",
                "Unable to resolve method of name 'MethodWithOverloads4' on type " +
                "'Avalonia.Markup.Xaml.UnitTests.Data.BindingTests_Method+ViewModel'. " +
                "Found 2 overloads accepting more than one parameter. " +
                "Expected a method with zero or one parameter.");
        }

        private static void AssertBindingFails(string methodName, string expectedError)
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var errors = new List<string>();
            using var logSink = TestLogSink.Start((level, area, _, template, values) =>
            {
                if (level >= LogEventLevel.Warning && area == LogArea.Binding)
                    errors.Add(template + " " + string.Join(" ", values));
            });

            var window = (Window)AvaloniaRuntimeXamlLoader.Load(
                $$"""
                  <Window xmlns='https://github.com/avaloniaui'
                          xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                      <Button Name='button' Command='{Binding {{methodName}}}' CommandParameter='foo' />
                  </Window>
                  """);
            var button = window.GetControl<Button>("button");
            var vm = new ViewModel();

            button.DataContext = vm;
            window.ApplyTemplate();

            Assert.Null(button.Command);
            Assert.Contains(errors, error => error.Contains(expectedError, StringComparison.Ordinal));
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
                var textBlock = window.GetControl<TextBlock>("textBlock");
                var vm = new ViewModel();

                textBlock.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(textBlock.Text);
            }
        }


        [Theory]
        [InlineData(null, "Not called")]
        [InlineData("A", "Do A")]
        public void Binding_Method_With_Parameter_To_Command_CanExecute(object? commandParameter, string result)
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
                var button = window.GetControl<Button>("button");
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
                var button = window.GetControl<Button>("button");
                var vm = new ViewModel()
                {
                    Parameter = null,
                };

                button.DataContext = vm;
                window.ApplyTemplate();

                Assert.NotNull(button.Command);

                Assert.Equal(button.IsEffectivelyEnabled, false);

                vm.Parameter = true;
                Threading.Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(button.IsEffectivelyEnabled, true);
            }
        }

        [Fact]
        public void Binding_Method_To_Command_Collected()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            WeakReference<ViewModel?> MakeRef()
            {
                var weakVm = new WeakReference<ViewModel?>(null);
                {
                    var vm = new ViewModel()
                    {
                        Parameter = null,
                    };
                    weakVm.SetTarget(vm);
                    var canExecuteCount = 0;
                    var action = new Action<object>(vm.Do);
                    var command = new Avalonia.Data.Converters.MethodToCommandConverter(action);
                    command.CanExecuteChanged += (s, e) => canExecuteCount++;
                    vm.Parameter = 0;
                    Threading.Dispatcher.UIThread.RunJobs();
                    vm.Parameter = null;
                    Threading.Dispatcher.UIThread.RunJobs();
                    Assert.Equal(2, canExecuteCount);
                }
                return weakVm;
            }
            bool IsAlive(WeakReference<ViewModel?> @ref)
            {
                return @ref.TryGetTarget(out var instance)
                    && instance is null == false;
            }

            var vmref = MakeRef();

            var beforeCollect = IsAlive(vmref);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var afterCollect = IsAlive(vmref);

            Assert.True(beforeCollect, "Invalid ViewModel instance, it is already collected.");
            Assert.False(afterCollect, "ViewModel instance was not collected");
        }

        static void PerformClick(Button button)
        {
            button.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Input.Key.Enter,
            });
        }

        private class ViewModelBase
        {
            public virtual void VirtualObjectMethod(object? i) { }

            public virtual void VirtualInt32Method(int i) { }

            public virtual void VirtualStringMethod(string i) { }

            public void MethodWithNewSlot(int i) { }
        }

        private class ViewModel : ViewModelBase, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public void Method() => Value = "Called";

            public void ObjectMethod(object i) => Value = $"Called ObjectMethod with {i}";

            public void Int32Method(int i) => Value = $"Called Int32Method with {i}";

            public void StringMethod(string i) => Value = $"Called StringMethod with {i}";

            public void MethodWithOverloads() => Value = "Called MethodWithOverloads without parameter";
            public void MethodWithOverloads(int i) => Value = $"Called MethodWithOverloads with Int32 {i}";
            public void MethodWithOverloads(string i) => Value = $"Called MethodWithOverloads with String {i}";
            public void MethodWithOverloads(object i) => Value = $"Called MethodWithOverloads with Object {i}";

            public void MethodWithOverloads2() => Value = "Called MethodWithOverloads2 without parameter";
            public void MethodWithOverloads2(int i) => Value = $"Called MethodWithOverloads2 with Int32 {i}";
            public void MethodWithOverloads2(string i) => Value = $"Called MethodWithOverloads2 with String {i}";

            public void MethodWithOverloads3() => Value = "Called MethodWithOverloads3 without parameter";
            public void MethodWithOverloads3(int a, int b) => throw new InvalidOperationException("MethodWithOverloads3 should not be called");
            public void MethodWithOverloads3(string a, string b) => throw new InvalidOperationException("MethodWithOverloads3 should not be called");

            public void MethodWithOverloads4(int a, int b) => throw new InvalidOperationException("MethodWithOverloads4 should not be called");
            public void MethodWithOverloads4(string a, string b) => throw new InvalidOperationException("MethodWithOverloads4 should not be called");

            public override void VirtualObjectMethod(object? i)
                => Value = $"Called VirtualObjectMethod with {i}";

            public override void VirtualInt32Method(int i)
                => Value = $"Called VirtualInt32Method with {i}";

            public override void VirtualStringMethod(string i)
                => Value = $"Called VirtualStringMethod with {i}";

            public new void MethodWithNewSlot(int i)
                => Value = $"Called MethodWithNewSlot with {i}";

            public string Value { get; private set; } = "Not called";

            private object? _parameter;
            public object? Parameter
            {
                get => _parameter;
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
