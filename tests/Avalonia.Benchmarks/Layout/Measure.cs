using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    [MemoryDiagnoser]
    public class Measure : IDisposable
    {
        private IDisposable _app;
        private TestRoot root;
        private List<Control> controls = new List<Control>();

        public Measure()
        {
            _app = UnitTestApplication.Start(TestServices.RealLayoutManager);

            var panel = new StackPanel();
            root = new TestRoot { Child = panel };
            controls.Add(panel);
            CreateChildren(panel, 3, 5);
            LayoutManager.Instance.ExecuteInitialLayoutPass(root);
        }

        public void Dispose()
        {
            _app.Dispose();
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

            LayoutManager.Instance.ExecuteLayoutPass();
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
