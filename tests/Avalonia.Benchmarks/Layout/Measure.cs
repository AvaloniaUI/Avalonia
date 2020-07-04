using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    [MemoryDiagnoser]
    public class Measure
    {
        private readonly TestRoot _root;
        private readonly List<Control> _controls = new List<Control>();

        public Measure()
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
        public void Remeasure()
        {
            foreach (var control in _controls)
            {
                control.InvalidateMeasure();
            }

            _root.LayoutManager.ExecuteLayoutPass();
        }
    }
}
