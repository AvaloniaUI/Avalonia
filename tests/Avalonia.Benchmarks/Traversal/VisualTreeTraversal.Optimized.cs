using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Traversal
{
    /// <summary>
    /// Benchmarks for optimized visual tree traversal methods.
    /// These benchmarks use struct enumerators and optimized algorithms
    /// introduced in the perf/visual-tree-optimizations branch.
    /// </summary>
    [MemoryDiagnoser]
    public class VisualTreeTraversalOptimized : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private List<Control> _controls = new List<Control>();

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            var panel = new StackPanel();
            _root = new TestRoot { Child = panel, Renderer = new NullRenderer() };
            _controls.Add(panel);
            _controls = ControlHierarchyCreator.CreateChildren(_controls, panel, 3, 5, 4);

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        public void Dispose()
        {
            _app?.Dispose();
        }

        [Benchmark(Description = "FindAncestorOfType using struct enumerator")]
        public void FindAncestorOfType_StructEnumerator()
        {
            foreach (Control control in _controls)
            {
                foreach (var ancestor in control.EnumerateSelfAndAncestors())
                {
                    if (ancestor is TestRoot root)
                    {
                        _ = root;
                        break;
                    }
                }
            }
        }

        [Benchmark(Description = "EnumerateAncestors iteration")]
        public void EnumerateAncestors()
        {
            foreach (Control control in _controls)
            {
                var count = 0;
                foreach (var ancestor in control.EnumerateAncestors())
                {
                    count++;
                }
            }
        }

        [Benchmark(Description = "EnumerateSelfAndAncestors iteration")]
        public void EnumerateSelfAndAncestors()
        {
            foreach (Control control in _controls)
            {
                var count = 0;
                foreach (var ancestor in control.EnumerateSelfAndAncestors())
                {
                    count++;
                }
            }
        }

        [Benchmark(Description = "GetVisualDescendants using struct enumerator")]
        public void GetVisualDescendants_StructEnumerator()
        {
            var count = 0;
            foreach (var descendant in _root.EnumerateDescendants())
            {
                count++;
            }
        }

        [Benchmark(Description = "SortByZIndex optimized (no allocations)")]
        public void SortByZIndex_Optimized()
        {
            var output = new List<Visual>();
            _root.VisualChildren.SortByZIndexInto(output);
        }

        [Benchmark(Description = "VisualLevel property access")]
        public void VisualLevel_Access()
        {
            var total = 0;
            foreach (Control control in _controls)
            {
                total += control.VisualLevel;
            }
        }
    }
}
