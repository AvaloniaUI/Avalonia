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
    /// Benchmarks for tree traversal patterns during layout.
    /// Analyzes the cost of different tree structures and traversal algorithms.
    /// </summary>
    [MemoryDiagnoser]
    public class TreeTraversalBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _deepTree = null!;
        private TestRoot _wideTree = null!;
        private TestRoot _balancedTree = null!;
        private List<Control> _deepTreeControls = null!;
        private List<Control> _wideTreeControls = null!;
        private List<Control> _balancedTreeControls = null!;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Deep tree: 50 levels deep, 1-2 children per level
            _deepTree = CreateDeepTree(50);
            _deepTreeControls = CollectAllControls(_deepTree);

            // Wide tree: 3 levels deep, many children per level
            _wideTree = CreateWideTree(3, 100);
            _wideTreeControls = CollectAllControls(_wideTree);

            // Balanced tree: moderate depth and width
            _balancedTree = CreateBalancedTree(5, 5);
            _balancedTreeControls = CollectAllControls(_balancedTree);
        }

        private static TestRoot CreateDeepTree(int depth)
        {
            var root = new TestRoot { Renderer = new NullRenderer() };
            
            Panel current = new StackPanel();
            root.Child = current;

            for (var i = 0; i < depth; i++)
            {
                var next = new StackPanel();
                current.Children.Add(new Button { Width = 50, Height = 20 });
                current.Children.Add(next);
                current = next;
            }

            // Add final leaf
            current.Children.Add(new Button { Width = 50, Height = 20 });

            root.LayoutManager.ExecuteInitialLayoutPass();
            return root;
        }

        private static TestRoot CreateWideTree(int depth, int childrenPerLevel)
        {
            var root = new TestRoot { Renderer = new NullRenderer() };
            
            var panel = new StackPanel();
            root.Child = panel;

            CreateWideChildren(panel, depth, childrenPerLevel);

            root.LayoutManager.ExecuteInitialLayoutPass();
            return root;
        }

        private static void CreateWideChildren(Panel parent, int depth, int childrenPerLevel)
        {
            if (depth <= 0)
            {
                for (var i = 0; i < childrenPerLevel; i++)
                {
                    parent.Children.Add(new Button { Width = 50, Height = 20 });
                }
                return;
            }

            for (var i = 0; i < childrenPerLevel; i++)
            {
                var panel = new StackPanel();
                parent.Children.Add(panel);
                CreateWideChildren(panel, depth - 1, Math.Max(1, childrenPerLevel / 3));
            }
        }

        private static TestRoot CreateBalancedTree(int depth, int childrenPerLevel)
        {
            var root = new TestRoot { Renderer = new NullRenderer() };
            
            var panel = new StackPanel();
            root.Child = panel;

            CreateBalancedChildren(panel, depth, childrenPerLevel);

            root.LayoutManager.ExecuteInitialLayoutPass();
            return root;
        }

        private static void CreateBalancedChildren(Panel parent, int depth, int childrenPerLevel)
        {
            if (depth <= 0)
            {
                parent.Children.Add(new Button { Width = 50, Height = 20 });
                return;
            }

            for (var i = 0; i < childrenPerLevel; i++)
            {
                var panel = new StackPanel();
                parent.Children.Add(panel);
                CreateBalancedChildren(panel, depth - 1, childrenPerLevel);
            }
        }

        private static List<Control> CollectAllControls(TestRoot root)
        {
            var controls = new List<Control>();
            
            void Collect(Control control)
            {
                controls.Add(control);
                if (control is Panel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        Collect(child);
                    }
                }
                else if (control is Decorator decorator && decorator.Child != null)
                {
                    Collect(decorator.Child);
                }
            }

            if (root.Child != null)
            {
                Collect(root.Child);
            }

            return controls;
        }

        /// <summary>
        /// Full layout pass on a deep tree (50 levels).
        /// Tests recursive upward traversal performance.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DeepTree_FullLayout()
        {
            InvalidateAll(_deepTreeControls);
            _deepTree.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Layout pass on deep tree with only deepest leaf invalidated.
        /// Tests the worst case for upward traversal.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DeepTree_SingleLeafInvalidation()
        {
            var leaf = _deepTreeControls[^1];
            SetIsMeasureValid(leaf, false);
            SetIsArrangeValid(leaf, false);
            _deepTree.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Full layout pass on a wide tree (100+ children per level).
        /// Tests performance with many siblings.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WideTree_FullLayout()
        {
            InvalidateAll(_wideTreeControls);
            _wideTree.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Layout pass on wide tree with all siblings at one level invalidated.
        /// Key scenario for topological sort optimization.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WideTree_AllSiblingsInvalidated()
        {
            if (_wideTree.Child is Panel panel)
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
            _wideTree.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Full layout pass on a balanced tree.
        /// Tests typical real-world tree structure.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BalancedTree_FullLayout()
        {
            InvalidateAll(_balancedTreeControls);
            _balancedTree.LayoutManager.ExecuteLayoutPass();
        }

        private static void InvalidateAll(List<Control> controls)
        {
            foreach (var control in controls)
            {
                if (control is Layoutable layoutable)
                {
                    SetIsMeasureValid(layoutable, false);
                    SetIsArrangeValid(layoutable, false);
                }
            }
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
