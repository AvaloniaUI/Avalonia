using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

public abstract partial class BindingExpressionTests
{
    [Fact]
    public void Should_Write_Value_To_Source()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTargetWithSource(data, o => o.StringValue, mode: BindingMode.TwoWay);

        target.String = "bar";

        Assert.Equal("bar", data.StringValue);
    }

    [Fact]
    public void Should_Write_Value_To_Attached_Property_On_Source()
    {
        var data = new AvaloniaObject();
        var target = CreateTargetWithSource(
            data, 
            o => o[DockPanel.DockProperty],
            mode: BindingMode.TwoWay,
            targetProperty: Control.TagProperty);

        target.Tag = Dock.Right;

        Assert.Equal(Dock.Right, data[DockPanel.DockProperty]);
    }

    [Fact]
    public void Should_Write_Indexed_Value_To_Source()
    {
        var data = new { Foo = new[] { "foo" } };
        var target = CreateTargetWithSource(data, o => o.Foo[0], mode: BindingMode.TwoWay);

        target.String = "bar";

        Assert.Equal("bar", data.Foo[0]);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Write_Value_To_Source_On_Simple_Property_Chain()
    {
        var data = new ViewModel { Next = new() { StringValue = "foo" } };
        var target = CreateTargetWithSource(data, o => o.Next!.StringValue, mode: BindingMode.TwoWay);

        target.String = "bar";

        Assert.Equal("bar", data.Next!.StringValue);
    }

    [Fact]
    public void Target_Value_Can_Be_Set_On_Broken_Chain()
    {
        var data = new ViewModel { Next = new() { StringValue = "foo" } };
        var target = CreateTargetWithSource(data, o => o.Next!.StringValue, mode: BindingMode.TwoWay);

        data.Next = null;
        target.String = "bar";

        Assert.Equal("bar", target.String);
    }

    [Fact]
    public void Should_Use_Converter_When_Writing_To_Source()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTargetWithSource(
            data,
            o => o.StringValue,
            mode: BindingMode.TwoWay,
            converter: new CaseConverter());

        target.String = "BaR";
        Assert.Equal("bar", data.StringValue);

        GC.KeepAlive(data);
    }

    [Fact]
    public void TwoWay_Binding_Should_Not_Write_Unchanged_Value_Back_To_Property_With_Converter()
    {
        var data = new Cat();
        var target = CreateTargetWithSource(
            data,
            o => o.WhiskerCount,
            converter: new CaseConverter(),
            mode: BindingMode.TwoWay);

        Assert.Equal(4, target.Int);
        Assert.Equal(9, data.Lives);

        data.WhiskerCount = 3;

        Assert.Equal(3, target.Int);
        Assert.Equal(8, data.Lives);

        GC.KeepAlive(data);
    }

    [Fact]
    public void TwoWay_Binding_Should_Not_Write_Source_Value_Back_To_Source_With_Non_Reversible_Converter()
    {
        var data = new ViewModel { StringValue = "foo" };
        var converter = new EqualsParameterConverter();
        var target = CreateTargetWithSource(
            data,
            o => o.StringValue,
            converter: converter,
            converterParameter: "foo",
            mode: BindingMode.TwoWay,
            targetProperty: TargetClass.BoolProperty);

        Assert.True(target.Bool);

        data.StringValue = "bar";

        Assert.False(target.Bool);
        Assert.Equal("bar", data.StringValue);
        Assert.Equal(0, converter.ConvertBackCount);

        GC.KeepAlive(data);
    }

    [Fact]
    public void TwoWay_Binding_Should_Not_Write_Default_Value_Back_To_Source_When_Converter_Returns_UnsetValue()
    {
        var data = new ViewModel { StringValue = "foo" };
        var converter = new EqualsParameterConverter { UnsetForNull = true };
        var target = CreateTargetWithSource(
            data,
            o => o.StringValue,
            converter: converter,
            converterParameter: "foo",
            mode: BindingMode.TwoWay,
            targetProperty: TargetClass.BoolProperty);

        Assert.True(target.Bool);

        data.StringValue = null;

        Assert.False(target.Bool);
        Assert.Null(data.StringValue);
        Assert.Equal(0, converter.ConvertBackCount);

        GC.KeepAlive(data);
    }

    [Fact]
    public void TwoWay_Binding_Should_Write_Coerced_Value_Back_To_Source()
    {
        var data = new ViewModel { IntValue = 5 };
        var target = CreateTargetWithSource(
            data,
            o => o.IntValue,
            mode: BindingMode.TwoWay,
            targetProperty: CoercedIntProperty);

        data.IntValue = 15;

        Assert.Equal(10, target.GetValue(CoercedIntProperty));
        Assert.Equal(10, data.IntValue);

        GC.KeepAlive(data);
    }

    [Fact]
    public void TwoWay_Binding_Should_Not_Write_Back_To_Source_When_Source_Loses_Its_DataContext()
    {
        var data = new ViewModel { StringValue = "foo" };
        var host = CreateTarget<ViewModel, string?>(
            o => o.StringValue,
            dataContext: data,
            mode: BindingMode.TwoWay);
        var converter = new EqualsParameterConverter();
        var target = CreateTargetWithSource(
            host,
            o => o.String,
            converter: converter,
            converterParameter: "foo",
            mode: BindingMode.TwoWay,
            targetProperty: TargetClass.BoolProperty);

        Assert.True(target.Bool);

        host.DataContext = null;

        Assert.False(target.Bool);
        Assert.Equal("foo", data.StringValue);
        Assert.Equal(0, converter.ConvertBackCount);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Setter_Should_Convert_Double_To_String()
    {
        var data = new ViewModel { StringValue = $"{5.6}" };
        var target = CreateTargetWithSource(
            data, 
            o => o.StringValue, 
            mode: BindingMode.TwoWay,
            targetProperty: TargetClass.DoubleProperty);

        target.Double = 6.7;

        Assert.Equal($"{6.7}", data.StringValue);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Setter_Should_Convert_String_To_Double()
    {
        var data = new ViewModel { DoubleValue = 5.6 };
        var target = CreateTargetWithSource(
            data, 
            o => o.DoubleValue,
            mode: BindingMode.TwoWay,
            targetProperty: TargetClass.StringProperty);

        target.String = $"{6.7}";

        Assert.Equal(6.7, data.DoubleValue);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Setting_Invalid_Double_String_Should_Not_Change_Target()
    {
        var data = new ViewModel { DoubleValue = 5.6 };
        var target = CreateTargetWithSource(
            data,
            o => o.DoubleValue,
            mode: BindingMode.TwoWay,
            targetProperty: TargetClass.StringProperty);

        target.String = "foo";

        Assert.Equal(5.6, data.DoubleValue);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Setting_Invalid_Double_String_Should_Use_FallbackValue()
    {
        var data = new ViewModel { DoubleValue = 5.6 };
        var target = CreateTargetWithSource(
            data,
            o => o.DoubleValue,
            mode: BindingMode.TwoWay,
            fallbackValue: 9.8,
            targetProperty: TargetClass.StringProperty);

        target.String = "foo";

        Assert.Equal(9.8, data.DoubleValue);

        GC.KeepAlive(data);
    }

    [Fact]
    public void OneWayToSource_Binding_Should_Write_Value_Set_By_Target_While_Data_Validation_Error_Is_Published()
    {
        var data = new ExceptionViewModel();
        var target = CreateTargetWithSource(
            data,
            o => o.MustBePositive,
            enableDataValidation: true,
            mode: BindingMode.OneWayToSource);

        target.Int = 5;

        Assert.Equal(5, data.MustBePositive);
        Assert.False(DataValidationErrors.GetHasErrors(target));

        target.PropertyChanged += (_, e) =>
        {
            if (e.Property == DataValidationErrors.HasErrorsProperty && e.NewValue is true)
                target.Int = 7;
        };

        target.Int = -5;

        Assert.Equal(7, target.Int);
        Assert.Equal(7, data.MustBePositive);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Disposing_TwoWay_Binding_While_It_Publishes_A_Value_Should_Not_Throw()
    {
        var source = new SourceControl { StringValue = "foo" };
        var target = new TargetClass();
        BindingExpression? expression = null;

        // Subscribed before the binding is, so this runs before the binding's own handler for the target property.
        target.PropertyChanged += (_, e) =>
        {
            if (e.Property == TargetClass.StringProperty)
                expression?.Dispose();
        };

        (_, expression) = CreateTargetAndExpression<SourceControl, string?>(
            o => o.StringValue,
            mode: BindingMode.TwoWay,
            source: source,
            target: target);

        Assert.Equal("foo", target.String);

        source.StringValue = "bar";

        Assert.Null(target.String);
        Assert.Equal("bar", source.StringValue);
    }

    [Fact]
    public void Should_Pass_ConverterParameter_To_Converter_ConvertBack()
    {
        var data = new ViewModel { StringValue = "Initial" };
        var converter = new PrefixConverter();
        var target = CreateTargetWithSource(
            data,
            o => o.StringValue,
            converter: converter,
            converterParameter: "foo",
            mode: BindingMode.TwoWay);

        target.String = "fooBar";

        Assert.Equal("Bar", data.StringValue);
    }

    private class Cat : NotifyingBase
    {
        private int _whiskerCount = 4;

        public int WhiskerCount
        {
            get => _whiskerCount;
            set
            {
                _whiskerCount = value;
                RaisePropertyChanged(nameof(WhiskerCount));
                --Lives;
            }
        }

        public int Lives { get; private set; } = 9;
    }

    private static readonly StyledProperty<int> CoercedIntProperty =
        AvaloniaProperty.Register<TargetClass, int>("CoercedInt", coerce: (_, value) => Math.Min(value, 10));

    private class EqualsParameterConverter : IValueConverter
    {
        public int ConvertBackCount { get; private set; }
        public bool UnsetForNull { get; init; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null && UnsetForNull)
                return AvaloniaProperty.UnsetValue;
            return Equals(value, parameter);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            ++ConvertBackCount;
            return value is true ? parameter : null;
        }
    }

    private class CaseConverter : IValueConverter
    {
        public static readonly CaseConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString()?.ToUpper();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString()?.ToLower();
        }
    }
}
