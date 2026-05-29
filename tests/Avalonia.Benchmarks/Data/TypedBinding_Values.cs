using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Data;

// Compares the steady-state cost of pushing values through the three kinds of binding to a
// DataContext property: see TypedBinding_Setup for a description of each kind.
//
// The binding is attached once and then 100 value changes are pushed through it. An `int` property
// is used so that the OneWay benchmarks highlight the per-value boxing that the typed expression
// avoids; the allocation column (from MemoryDiagnoser) is the interesting one here.
[MemoryDiagnoser]
public class TypedBinding_Values
{
    private TestData _data = null!;
    private TestControl _target = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = new TestData();
        _target = new TestControl { DataContext = _data };
    }

    [Benchmark]
    public void Produce_Typed_OneWay()
    {
        _data.IntValue = -1;
        using var d = _target.Bind(TestControl.IntValueProperty, CreateTypedBinding(BindingMode.OneWay));

        for (var i = 0; i < 100; ++i)
            _data.IntValue = i;
    }

    [Benchmark]
    public void Produce_CompiledBinding_OneWay()
    {
        _data.IntValue = -1;
        using var d = _target.Bind(TestControl.IntValueProperty, CreateCompiledBinding(BindingMode.OneWay));

        for (var i = 0; i < 100; ++i)
            _data.IntValue = i;
    }

    [Benchmark]
    public void Produce_Reflection_OneWay()
    {
        _data.IntValue = -1;
        using var d = _target.Bind(TestControl.IntValueProperty, CreateReflectionBinding(BindingMode.OneWay));

        for (var i = 0; i < 100; ++i)
            _data.IntValue = i;
    }

    [Benchmark]
    public void Produce_Typed_TwoWay()
    {
        _data.IntValue = -1;
        using var d = _target.Bind(TestControl.IntValueProperty, CreateTypedBinding(BindingMode.TwoWay));

        for (var i = 0; i < 100; ++i)
        {
            _data.IntValue = i * 2;
            _target.IntValue = (i * 2) + 1;
        }
    }

    [Benchmark]
    public void Produce_CompiledBinding_TwoWay()
    {
        _data.IntValue = -1;
        using var d = _target.Bind(TestControl.IntValueProperty, CreateCompiledBinding(BindingMode.TwoWay));

        for (var i = 0; i < 100; ++i)
        {
            _data.IntValue = i * 2;
            _target.IntValue = (i * 2) + 1;
        }
    }

    [Benchmark]
    public void Produce_Reflection_TwoWay()
    {
        _data.IntValue = -1;
        using var d = _target.Bind(TestControl.IntValueProperty, CreateReflectionBinding(BindingMode.TwoWay));

        for (var i = 0; i < 100; ++i)
        {
            _data.IntValue = i * 2;
            _target.IntValue = (i * 2) + 1;
        }
    }

    private static CompiledBinding CreateTypedBinding(BindingMode mode)
    {
        var propertyInfo = new ClrPropertyInfo<TestData, int>(
            nameof(TestData.IntValue),
            v => v.IntValue,
            (o, v) => o.IntValue = v);
        var path = new CompiledBindingPathBuilder().Property(propertyInfo).Build();
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

    private static Binding CreateReflectionBinding(BindingMode mode)
        => new(nameof(TestData.IntValue)) { Mode = mode };

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
