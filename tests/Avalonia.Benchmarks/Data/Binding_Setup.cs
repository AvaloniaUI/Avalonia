using Avalonia.Controls;
using Avalonia.Data;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data;

[MemoryDiagnoser]
public class Binding_Setup
{
    private TestData _data = new();
    private TestControl _target = new();

    public Binding_Setup()
    {
        _target.DataContext = _data;
    }

    [Benchmark]
    public void Setup_DataContext_Property_Binding_OneWay()
    {
        var target = _target;
        var binding = new Binding(nameof(_data.IntValue));

        for (var i = 0; i < 100; ++i)
        {
            using var d = target.Bind(TestControl.IntValueProperty, binding);
        }
    }

    [Benchmark]
    public void Setup_DataContext_Property_Binding_TwoWay()
    {
        var target = _target;
        var binding = new Binding(nameof(_data.IntValue), mode: BindingMode.TwoWay);

        for (var i = 0; i < 100; ++i)
        {
            using var d = target.Bind(TestControl.IntValueProperty, binding);
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
