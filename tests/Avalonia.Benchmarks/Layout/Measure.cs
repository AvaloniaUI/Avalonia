using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
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
            _root.InvalidateMeasure();

            foreach (var control in _controls)
            {
                // Use an unsafe accessor instead of InvalidateMeasure, otherwise a lot of time is spent invalidating
                // controls, which we don't want: this benchmark is supposed to be focused on Measure/Arrange.
                SetIsMeasureValid(control, false);
                SetIsArrangeValid(control, false);
            }

            _root.LayoutManager.ExecuteLayoutPass();
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_" + nameof(Layoutable.IsMeasureValid))]
        private static extern void SetIsMeasureValid(Layoutable layoutable, bool value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_" + nameof(Layoutable.IsArrangeValid))]
        private static extern void SetIsArrangeValid(Layoutable layoutable, bool value);
    }
}
