using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for LayoutManager performance analysis.
    /// Tests the core layout orchestration including measure/arrange passes,
    /// tree traversal patterns, and invalidation handling.
    /// </summary>
    [MemoryDiagnoser]
    public class LayoutManagerBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private List<Control> _allControls = null!;
        private List<Control> _leafControls = null!;
        private Control _deepLeaf = null!;

        [Params(100, 500, 1000)]
        public int ControlCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot
            {
                Renderer = new NullRenderer()
            };

            var panel = new StackPanel();
            _root.Child = panel;

            _allControls = new List<Control> { panel };
            _leafControls = new List<Control>();

            // Create a tree structure with the specified number of controls
            CreateControlTree(panel, ControlCount, _allControls, _leafControls);

            _deepLeaf = _leafControls.Count > 0 ? _leafControls[^1] : panel;

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static void CreateControlTree(Panel parent, int totalControls, List<Control> allControls, List<Control> leafControls)
        {
            var remaining = totalControls - 1; // -1 for the root panel
            var depth = 0;
            var currentLevel = new List<Panel> { parent };

            while (remaining > 0 && depth < 20)
            {
                var nextLevel = new List<Panel>();
                var controlsPerParent = Math.Max(1, remaining / Math.Max(1, currentLevel.Count * 3));

                foreach (var p in currentLevel)
                {
                    for (var i = 0; i < 3 && remaining > 0; i++)
                    {
                        if (i < 2 && remaining > controlsPerParent)
                        {
                            var stack = new StackPanel();
                            p.Children.Add(stack);
                            allControls.Add(stack);
                            nextLevel.Add(stack);
                        }
                        else
                        {
                            var button = new Button { Width = 100, Height = 30 };
                            p.Children.Add(button);
                            allControls.Add(button);
                            leafControls.Add(button);
                        }
                        remaining--;
                    }
                }

                currentLevel = nextLevel;
                depth++;
            }
        }

        /// <summary>
        /// Measures the cost of a full layout pass with all controls invalidated.
        /// This represents the worst-case scenario where the entire tree needs relayout.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FullLayoutPass_AllInvalidated()
        {
            // Invalidate all controls
            foreach (var control in _allControls)
            {
                SetIsMeasureValid(control, false);
                SetIsArrangeValid(control, false);
            }

            _root.InvalidateMeasure();
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost when only leaf controls are invalidated.
        /// Tests how well the layout manager handles localized changes.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LayoutPass_OnlyLeavesInvalidated()
        {
            foreach (var control in _leafControls)
            {
                SetIsMeasureValid(control, false);
                SetIsArrangeValid(control, false);
            }

            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of invalidating and relaying out a single deep control.
        /// Tests the upward tree traversal efficiency.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LayoutPass_SingleDeepControl()
        {
            SetIsMeasureValid(_deepLeaf, false);
            SetIsArrangeValid(_deepLeaf, false);

            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of multiple sibling invalidations.
        /// This is a key scenario that could benefit from topological sorting.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LayoutPass_ManySiblings()
        {
            // Get siblings from the first panel
            if (_root.Child is Panel panel && panel.Children.Count > 0)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Layoutable layoutable)
                    {
                        SetIsMeasureValid(layoutable, false);
                        SetIsArrangeValid(layoutable, false);
                    }
                }
            }

            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the overhead of ExecuteLayoutPass when nothing needs layout.
        /// Tests the "no-op" performance.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LayoutPass_NoInvalidations()
        {
            _root.LayoutManager.ExecuteLayoutPass();
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_" + nameof(Layoutable.IsMeasureValid))]
        private static extern void SetIsMeasureValid(Layoutable layoutable, bool value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_" + nameof(Layoutable.IsArrangeValid))]
        private static extern void SetIsArrangeValid(Layoutable layoutable, bool value);

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
