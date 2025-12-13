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
    public class VisualTreeTraversal : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private List<Control> _controls = new List<Control>();
        private List<Control> _shuffledControls = null!;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            var panel = new StackPanel();
            _root = new TestRoot { Child = panel, Renderer = new NullRenderer() };
            _controls.Add(panel);
            _controls = ControlHierarchyCreator.CreateChildren(_controls, panel, 3, 5, 4);

            var random = new Random(1);

            _shuffledControls = _controls.OrderBy(r => random.Next()).ToList();

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        public void Dispose()
        {
            _app?.Dispose();
        }

        [Benchmark]
        public void FindAncestorOfType()
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

        [Benchmark]
        public void IsVisualAncestorOf()
        {
            foreach (Visual first in _controls)
            {
                foreach (Control second in _shuffledControls)
                {
                    first.IsVisualAncestorOf(second);
                }
            }
        }

        [Benchmark]
        public void GetVisualDescendants()
        {
            var count = 0;
            foreach (var descendant in _root.GetVisualDescendants())
            {
                count++;
            }
        }

        [Benchmark]
        public void SortByZIndex()
        {
            var sorted = _root.VisualChildren.SortByZIndex().ToList();
        }
    }
}
