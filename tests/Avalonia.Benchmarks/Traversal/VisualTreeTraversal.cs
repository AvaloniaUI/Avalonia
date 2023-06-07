using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Traversal
{
    [MemoryDiagnoser]
    public class VisualTreeTraversal
    {
        private readonly TestRoot _root;
        private readonly List<Control> _controls = new List<Control>();
        private readonly List<Control> _shuffledControls;

        public VisualTreeTraversal()
        {
            var panel = new StackPanel();
            _root = new TestRoot { Child = panel, Renderer = new NullRenderer()};
            _controls.Add(panel);
            _controls = ControlHierarchyCreator.CreateChildren(_controls, panel, 3, 5, 4);

            var random = new Random(1);

            _shuffledControls = _controls.OrderBy(r => random.Next()).ToList();

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [Benchmark]
        public void FindAncestorOfType_Linq()
        {
            foreach (Control control in _controls)
            {
                control.GetSelfAndVisualAncestors()
                    .OfType<TestRoot>()
                    .FirstOrDefault();
            }
        }

        [Benchmark]
        public void FindAncestorOfType_Optimized()
        {
            foreach (Control control in _controls)
            {
                control.FindAncestorOfType<TestRoot>();
            }
        }

        [Benchmark]
        public void FindCommonVisualAncestor()
        {
            foreach (Visual first in _controls)
            {
                foreach (Control second in _shuffledControls)
                {
                    first.FindCommonVisualAncestor(second);
                }
            }
        }
    }
}
