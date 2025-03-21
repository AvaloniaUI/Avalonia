using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data;

[MemoryDiagnoser]
public class InheritedProperties
{
    private readonly TestRoot _root;
    private readonly List<Control> _controls = new();

    public InheritedProperties()
    {
        var panel = new StackPanel();

        _root = new TestRoot
        {
            Child = panel,
            Renderer = new NullRenderer()
        };

        _controls.Add(panel);
        _controls = ControlHierarchyCreator.CreateChildren(_controls, panel, 3, 5, 5);

        _root.LayoutManager.ExecuteInitialLayoutPass();
    }
    
    [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
    public void ChangeDataContext()
    {
        TestDataContext[] dataContexts = [new(), new(), new()];
            
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < dataContexts.Length; j++)
            {
                _root.DataContext = dataContexts[j];
            }
        }
    }

    public class TestDataContext;
}
