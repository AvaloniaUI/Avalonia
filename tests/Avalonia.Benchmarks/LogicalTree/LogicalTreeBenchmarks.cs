using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.LogicalTree
{
    [MemoryDiagnoser]
    public class LogicalTreeBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private Control? _deepestControl;

        [Params(5, 10, 20)]
        public int TreeDepth { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };

            // Build a logical tree of nested StackPanels
            Control current = new StackPanel();
            _root.Child = current;

            for (int i = 0; i < TreeDepth; i++)
            {
                var child = new StackPanel();
                ((StackPanel)current).Children.Add(child);
                current = child;
            }

            _deepestControl = current;
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark getting logical parent chain
        /// </summary>
        [Benchmark(Baseline = true)]
        public int GetLogicalAncestors()
        {
            int count = 0;
            foreach (var ancestor in _deepestControl!.GetLogicalAncestors())
            {
                count++;
            }
            return count;
        }

        /// <summary>
        /// Benchmark getting self and logical ancestors
        /// </summary>
        [Benchmark]
        public int GetSelfAndLogicalAncestors()
        {
            int count = 0;
            foreach (var ancestor in _deepestControl!.GetSelfAndLogicalAncestors())
            {
                count++;
            }
            return count;
        }

        /// <summary>
        /// Benchmark finding ancestor of specific type
        /// </summary>
        [Benchmark]
        public StyledElement? FindLogicalAncestorOfType()
        {
            return _deepestControl!.FindLogicalAncestorOfType<TestRoot>();
        }

        /// <summary>
        /// Benchmark getting logical children
        /// </summary>
        [Benchmark]
        public int GetLogicalChildren()
        {
            int count = 0;
            foreach (var child in _root!.GetLogicalChildren())
            {
                count++;
            }
            return count;
        }

        /// <summary>
        /// Benchmark getting logical descendants
        /// </summary>
        [Benchmark]
        public int GetLogicalDescendants()
        {
            int count = 0;
            foreach (var descendant in _root!.GetLogicalDescendants())
            {
                count++;
            }
            return count;
        }

        /// <summary>
        /// Benchmark finding named element (common pattern in code-behind)
        /// </summary>
        [Benchmark]
        public Control? FindNamedElement()
        {
            return _root!.Find<Control>("nonexistent");
        }
    }
}
