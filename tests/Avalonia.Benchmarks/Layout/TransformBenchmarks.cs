using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for transform-related layout operations.
    /// Tests TransformToVisual and bounds calculations.
    /// </summary>
    [MemoryDiagnoser]
    public class TransformBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _simpleRoot = null!;
        private TestRoot _transformedRoot = null!;
        private TestRoot _deepTransformedRoot = null!;
        private Control _simpleLeaf = null!;
        private Control _transformedLeaf = null!;
        private Control _deepTransformedLeaf = null!;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Simple hierarchy (no transforms)
            _simpleRoot = new TestRoot { Renderer = new NullRenderer() };
            _simpleLeaf = CreateSimpleHierarchy();
            _simpleRoot.Child = _simpleLeaf is Panel ? _simpleLeaf : new Border { Child = _simpleLeaf };
            _simpleRoot.LayoutManager.ExecuteInitialLayoutPass();

            // Hierarchy with transforms at each level
            _transformedRoot = new TestRoot { Renderer = new NullRenderer() };
            var (transformedPanel, transformedLeaf) = CreateTransformedHierarchy(5);
            _transformedRoot.Child = transformedPanel;
            _transformedLeaf = transformedLeaf;
            _transformedRoot.LayoutManager.ExecuteInitialLayoutPass();

            // Deep hierarchy with transforms
            _deepTransformedRoot = new TestRoot { Renderer = new NullRenderer() };
            var (deepPanel, deepLeaf) = CreateTransformedHierarchy(15);
            _deepTransformedRoot.Child = deepPanel;
            _deepTransformedLeaf = deepLeaf;
            _deepTransformedRoot.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static Control CreateSimpleHierarchy()
        {
            var root = new StackPanel();
            Panel current = root;

            for (var i = 0; i < 10; i++)
            {
                var next = new StackPanel();
                current.Children.Add(next);
                current = next;
            }

            var leaf = new Button { Width = 50, Height = 30, Content = "Leaf" };
            current.Children.Add(leaf);
            return root;
        }

        private static (Control, Control) CreateTransformedHierarchy(int depth)
        {
            var root = new Border
            {
                RenderTransform = new TranslateTransform(10, 10),
                Width = 800,
                Height = 600
            };

            Control current = root;
            for (var i = 0; i < depth; i++)
            {
                var remainder = i % 3;
                var wrapper = new Border
                {
                    RenderTransform = remainder switch
                    {
                        0 => (Transform)new TranslateTransform(5, 5),
                        1 => new RotateTransform(5),
                        _ => new ScaleTransform(0.95, 0.95)
                    },
                    RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    Padding = new Thickness(5)
                };

                if (current is Border border)
                    border.Child = wrapper;
                else if (current is Panel panel)
                    panel.Children.Add(wrapper);

                current = wrapper;
            }

            var leaf = new Button { Width = 50, Height = 30, Content = "Leaf" };
            if (current is Border b)
                b.Child = leaf;
            else if (current is Panel p)
                p.Children.Add(leaf);

            return (root, leaf);
        }

        /// <summary>
        /// TransformToVisual with no transforms in hierarchy.
        /// Baseline for transform operations.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Matrix? TransformToVisual_NoTransforms()
        {
            return _simpleLeaf.TransformToVisual(_simpleRoot);
        }

        /// <summary>
        /// TransformToVisual with transforms (5 levels).
        /// Tests matrix accumulation cost.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Matrix? TransformToVisual_WithTransforms()
        {
            return _transformedLeaf.TransformToVisual(_transformedRoot);
        }

        /// <summary>
        /// TransformToVisual with deep transforms (15 levels).
        /// Tests scaling behavior.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Matrix? TransformToVisual_DeepTransforms()
        {
            return _deepTransformedLeaf.TransformToVisual(_deepTransformedRoot);
        }

        /// <summary>
        /// Multiple TransformToVisual calls (simulates hit testing).
        /// Tests repeated transformation lookups.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MultipleTransformCalls()
        {
            for (var i = 0; i < 100; i++)
            {
                _ = _transformedLeaf.TransformToVisual(_transformedRoot);
            }
        }

        /// <summary>
        /// Point transformation through hierarchy.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Point? TransformPoint()
        {
            var transform = _transformedLeaf.TransformToVisual(_transformedRoot);
            if (transform.HasValue)
            {
                return new Point(25, 15).Transform(transform.Value);
            }
            return null;
        }

        /// <summary>
        /// Bounds calculation with transforms.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Rect TransformBounds()
        {
            var bounds = _transformedLeaf.Bounds;
            var transform = _transformedLeaf.TransformToVisual(_transformedRoot);
            if (transform.HasValue)
            {
                return bounds.TransformToAABB(transform.Value);
            }
            return bounds;
        }

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
