using Avalonia.Controls;
using Avalonia.Data;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data;

[MemoryDiagnoser]
public class TemplateBinding_Values
{
    private Decorator _target = new();
    private Control _templatedParent = new();

    public TemplateBinding_Values()
    {
        _target.TemplatedParent = _templatedParent;
    }

    [Benchmark]
    public void Produce_TemplateBinding_Value_OneWay()
    {
        var target = _target;
        var binding = new TemplateBinding(Control.TagProperty);

        // Explicit cast to IBinding is required to prevent the IObservable<object?>
        // overload being selected.
        using var d = target.Bind(Control.TagProperty, (IBinding)binding);

        for (var i = 0; i < 100; ++i)
        {
            _templatedParent.Tag = i;
        }
    }

    [Benchmark]
    public void Produce_TemplateBinding_Value_TwoWay()
    {
        var target = _target;
        var binding = new TemplateBinding(Control.TagProperty) { Mode = BindingMode.TwoWay };

        // Explicit cast to IBinding is required to prevent the IObservable<object?>
        // overload being selected.
        using var d = target.Bind(Control.TagProperty, (IBinding)binding);

        for (var i = 0; i < 100; ++i)
        {
            _templatedParent.Tag = i * 2;
            target.SetCurrentValue(Control.TagProperty, (i * 2) + 1);
        }
    }
}
