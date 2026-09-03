using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Data;

// Compares the setup (create instance + attach + dispose) cost of the three kinds of binding to
// a DataContext property:
//
//  - Typed:          the new TypedBindingExpression (built here by hand as the XAML compiler does
//                    not yet produce it). Strongly typed, does not box.
//  - CompiledBinding: the untyped CompiledBinding that the XAML compiler produces today for
//                    `{Binding Value}` (uses an InpcPropertyAccessor + boxed values).
//  - Reflection:     the classic reflection-based `Binding`.
//
// An `int` property is used as boxing avoidance is the main selling point of the typed expression.
// The binding instances are created once and reused across the loop (as they are in real XAML)
// so that only the per-bind cost is measured, not path/expression construction.
[MemoryDiagnoser]
public class TypedBinding_Setup
{
    private readonly TestData _data = new();
    private readonly TestControl _target = new();
    private readonly CompiledBinding _typedOneWay = CreateTypedBinding(BindingMode.OneWay);
    private readonly CompiledBinding _typedTwoWay = CreateTypedBinding(BindingMode.TwoWay);
    private readonly CompiledBinding _compiledOneWay = CreateCompiledBinding(BindingMode.OneWay);
    private readonly CompiledBinding _compiledTwoWay = CreateCompiledBinding(BindingMode.TwoWay);
    private readonly Binding _reflectionOneWay = new(nameof(TestData.IntValue)) { Mode = BindingMode.OneWay };
    private readonly Binding _reflectionTwoWay = new(nameof(TestData.IntValue)) { Mode = BindingMode.TwoWay };

    public TypedBinding_Setup()
    {
        _target.DataContext = _data;
    }

    [Benchmark]
    public void Setup_Typed_OneWay()
    {
        for (var i = 0; i < 100; ++i)
            using (_target.Bind(TestControl.IntValueProperty, _typedOneWay)) { }
    }

    [Benchmark]
    public void Setup_CompiledBinding_OneWay()
    {
        for (var i = 0; i < 100; ++i)
            using (_target.Bind(TestControl.IntValueProperty, _compiledOneWay)) { }
    }

    [Benchmark]
    public void Setup_Reflection_OneWay()
    {
        for (var i = 0; i < 100; ++i)
            using (_target.Bind(TestControl.IntValueProperty, _reflectionOneWay)) { }
    }

    [Benchmark]
    public void Setup_Typed_TwoWay()
    {
        for (var i = 0; i < 100; ++i)
            using (_target.Bind(TestControl.IntValueProperty, _typedTwoWay)) { }
    }

    [Benchmark]
    public void Setup_CompiledBinding_TwoWay()
    {
        for (var i = 0; i < 100; ++i)
            using (_target.Bind(TestControl.IntValueProperty, _compiledTwoWay)) { }
    }

    [Benchmark]
    public void Setup_Reflection_TwoWay()
    {
        for (var i = 0; i < 100; ++i)
            using (_target.Bind(TestControl.IntValueProperty, _reflectionTwoWay)) { }
    }

    private static CompiledBinding CreateTypedBinding(BindingMode mode)
    {
        var propertyInfo = new ClrPropertyInfo<TestData, int>(
            nameof(TestData.IntValue),
            v => v.IntValue,
            (o, v) => o.IntValue = v);
        var path = new CompiledBindingPathBuilder().Property(
            propertyInfo,
            PropertyInfoAccessorFactory.CreateInpcPropertyAccessor,
            false).Build();
        return new CompiledBinding(path) { Mode = mode };
    }

    // Builds the untyped CompiledBinding the same way the XAML compiler does today: a
    // CompiledBindingPath with an (object-typed) ClrPropertyInfo accessed via an
    // InpcPropertyAccessor. Differs from the typed binding only in the path element used.
    private static CompiledBinding CreateCompiledBinding(BindingMode mode)
    {
        var propertyInfo = new ClrPropertyInfo(
            nameof(TestData.IntValue),
            o => ((TestData)o).IntValue,
            (o, v) => ((TestData)o).IntValue = (int)v!,
            typeof(int));
        var path = new CompiledBindingPathBuilder()
            .Property(propertyInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
            .Build();
        return new CompiledBinding(path) { Mode = mode };
    }

    private class TestControl : Control
    {
        public static readonly StyledProperty<int> IntValueProperty =
            AvaloniaProperty.Register<TestControl, int>(nameof(IntValue));

        public int IntValue
        {
            get => GetValue(IntValueProperty);
            set => SetValue(IntValueProperty, value);
        }
    }

    private class TestData : INotifyPropertyChanged
    {
        private int _intValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int IntValue
        {
            get => _intValue;
            set
            {
                if (_intValue == value)
                    return;
                _intValue = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IntValue)));
            }
        }
    }
}
