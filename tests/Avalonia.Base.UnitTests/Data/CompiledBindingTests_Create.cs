using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data;

public class CompiledBindingTests_Create
{
    [Fact]
    public void Create_Should_Create_Binding_With_Simple_Property()
    {
        var binding = CompiledBinding.Create<TestViewModel, string?>(vm => vm.StringProperty);

        Assert.NotNull(binding);
        Assert.NotNull(binding.Path);
        Assert.Equal("StringProperty", binding.Path.ToString());
        Assert.Equal(AvaloniaProperty.UnsetValue, binding.Source);
        Assert.Equal(BindingMode.Default, binding.Mode);
    }

    [Fact]
    public void Create_Should_Create_Binding_With_Source()
    {
        var source = new TestViewModel { StringProperty = "Test" };
        var binding = CompiledBinding.Create<TestViewModel, string?>(
            vm => vm.StringProperty,
            source: source);

        Assert.NotNull(binding);
        Assert.NotNull(binding.Path);
        Assert.Equal("StringProperty", binding.Path.ToString());
        Assert.Same(source, binding.Source);
    }

    [Fact]
    public void Create_Should_Apply_Converter()
    {
        var converter = new TestConverter();
        var binding = CompiledBinding.Create<TestViewModel, string?>(
            vm => vm.StringProperty,
            converter: converter);

        Assert.Same(converter, binding.Converter);
    }

    [Fact]
    public void Create_Should_Apply_Mode()
    {
        var binding = CompiledBinding.Create<TestViewModel, string?>(
            vm => vm.StringProperty,
            mode: BindingMode.TwoWay);

        Assert.Equal(BindingMode.TwoWay, binding.Mode);
    }

    [Fact]
    public void Create_Should_Work_With_Nested_Properties()
    {
        var binding = CompiledBinding.Create<TestViewModel, string?>(
            vm => vm.Child!.StringProperty);

        Assert.NotNull(binding);
        Assert.NotNull(binding.Path);
        Assert.Equal("Child.StringProperty", binding.Path.ToString());
    }

    [Fact]
    public void Create_Should_Work_With_Indexer()
    {
        var binding = CompiledBinding.Create<TestViewModel, string?>(
            vm => vm.Items[0]);

        Assert.NotNull(binding);
        Assert.NotNull(binding.Path);
        Assert.Equal("Items[0]", binding.Path.ToString());
    }

    [Fact]
    public void Binding_Should_Work_When_Applied_To_Control()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var target = new TextBlock();
            var viewModel = new TestViewModel { StringProperty = "Hello" };
            var binding = CompiledBinding.Create<TestViewModel, string?>(
                vm => vm.StringProperty,
                source: viewModel);

            target.Bind(TextBlock.TextProperty, binding);

            Assert.Equal("Hello", target.Text);
        }
    }

    [Fact]
    public void Binding_Should_Update_When_Source_Property_Changes()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var target = new TextBlock();
            var viewModel = new TestViewModel { StringProperty = "Initial" };
            var binding = CompiledBinding.Create<TestViewModel, string?>(
                vm => vm.StringProperty,
                source: viewModel);

            target.Bind(TextBlock.TextProperty, binding);
            Assert.Equal("Initial", target.Text);

            viewModel.StringProperty = "Updated";
            Assert.Equal("Updated", target.Text);
        }
    }

    [Fact]
    public void Binding_Should_Use_DataContext_When_No_Source_Specified()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var target = new TextBlock();
            var viewModel = new TestViewModel { StringProperty = "FromDataContext" };
            var binding = CompiledBinding.Create<TestViewModel, string?>(vm => vm.StringProperty);

            target.DataContext = viewModel;
            target.Bind(TextBlock.TextProperty, binding);

            Assert.Equal("FromDataContext", target.Text);
        }
    }

    private class TestViewModel : NotifyingBase
    {
        private string? _stringProperty;
        private TestViewModel? _child;

        public string? StringProperty
        {
            get => _stringProperty;
            set { _stringProperty = value; RaisePropertyChanged(); }
        }

        public TestViewModel? Child
        {
            get => _child;
            set { _child = value; RaisePropertyChanged(); }
        }

        public string?[] Items { get; set; } = Array.Empty<string?>();
    }

    private class TestConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value?.ToString()?.ToUpper();

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value?.ToString()?.ToLower();
    }
}
