using Avalonia.Controls;
using Avalonia.Data;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data;

[MemoryDiagnoser]
public class Binding_Values
{
    private TestData _data = new();
    private TestControl _target = new();

    public Binding_Values()
    {
        _target.DataContext = _data;
    }

    [Benchmark]
    public void Produce_DataContext_Property_Binding_Value_OneWay()
    {
        _data.IntValue = -1;

        var target = _target;
        var binding = new Binding(nameof(_data.IntValue));
        using var d = target.Bind(TestControl.IntValueProperty, binding);

        for (var i = 0; i < 100; ++i)
        {
            _data.IntValue = i;
        }
    }

    [Benchmark]
    public void Produce_DataContext_Property_Binding_Value_TwoWay()
    {
        _data.IntValue = -1;

        var target = _target;
        var binding = new Binding(nameof(_data.IntValue), mode: BindingMode.TwoWay);
        using var d = target.Bind(TestControl.IntValueProperty, binding);

        for (var i = 0; i < 100; ++i)
        {
            _data.IntValue = i * 2;
            target.IntValue = (i * 2) + 1;
        }
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

    private class TestData
    {
        public int IntValue { get; set; }
    }
}
