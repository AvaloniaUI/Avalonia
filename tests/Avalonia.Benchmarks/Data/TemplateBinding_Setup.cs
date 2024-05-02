using Avalonia.Controls;
using Avalonia.Data;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data;

[MemoryDiagnoser]
public class TemplateBinding_Setup
{
    private Decorator _target = new();
    private Control _templatedParent = new();

    public TemplateBinding_Setup()
    {
        _target.TemplatedParent = _templatedParent;
        _templatedParent.Tag = "parentTag";
    }

    [Benchmark]
    public void Setup_TemplateBinding_OneWay()
    {
        var target = _target;
        var binding = new TemplateBinding(Control.TagProperty);

        for (var i = 0; i < 100; ++i)
        {
            // Explicit cast to IBinding is required to prevent the IObservable<object?>
            // overload being selected.
            using var d = target.Bind(Control.TagProperty, (IBinding)binding);
        }
    }

    [Benchmark]
    public void Setup_TemplateBinding_TwoWay()
    {
        var target = _target;
        var binding = new TemplateBinding(Control.TagProperty) { Mode = BindingMode.TwoWay };

        for (var i = 0; i < 100; ++i)
        {
            // Explicit cast to IBinding is required to prevent the IObservable<object?>
            // overload being selected.
            using var d = target.Bind(Control.TagProperty, (IBinding)binding);
        }
    }
}
