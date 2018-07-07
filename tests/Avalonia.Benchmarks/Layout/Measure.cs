using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    [MemoryDiagnoser]
    public class Measure
    {
        private TestRoot root;
        private List<Control> controls = new List<Control>();

        public Measure()
        {
            var panel = new StackPanel();
            root = new TestRoot { Child = panel };
            controls.Add(panel);
            CreateChildren(panel, 3, 5);
            root.LayoutManager.ExecuteInitialLayoutPass(root);
        }

        [Benchmark]
        public void Remeasure_Half()
        {
            var random = new Random(1);

            foreach (var control in controls)
            {
                if (random.Next(2) == 0)
                {
                    control.InvalidateMeasure();
                }
            }

            root.LayoutManager.ExecuteLayoutPass();
        }

        private void CreateChildren(IPanel parent, int childCount, int iterations)
        {
            for (var i = 0; i < childCount; ++i)
            {
                var control = new StackPanel();
                parent.Children.Add(control);

                if (iterations > 0)
                {
                    CreateChildren(control, childCount, iterations - 1);
                }

                controls.Add(control);
            }
        }
    }
}
