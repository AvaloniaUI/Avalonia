using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for Canvas layout performance.
    /// Canvas has simple layout but uses attached properties for positioning.
    /// </summary>
    [MemoryDiagnoser]
    public class CanvasBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private Canvas _canvas = null!;

        [Params(50, 200, 500)]
        public int ChildCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot { Renderer = new NullRenderer() };
            _canvas = new Canvas { Width = 1000, Height = 1000 };

            var random = new Random(42); // Fixed seed for reproducibility
            for (var i = 0; i < ChildCount; i++)
            {
                var button = new Button
                {
                    Width = 80,
                    Height = 25,
                    Content = $"B{i}"
                };
                Canvas.SetLeft(button, random.NextDouble() * 900);
                Canvas.SetTop(button, random.NextDouble() * 900);
                _canvas.Children.Add(button);
            }

            _root.Child = _canvas;
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        /// <summary>
        /// Measures Canvas layout performance.
        /// Canvas should be O(n) with minimal overhead.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Canvas_Measure()
        {
            _canvas.InvalidateMeasure();
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Full layout pass with all children invalidated.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Canvas_FullLayout()
        {
            InvalidateRecursive(_canvas);
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of moving a child (changing attached properties).
        /// Tests attached property lookup overhead.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Canvas_MoveChild()
        {
            if (_canvas.Children.Count > 0 && _canvas.Children[0] is Control child)
            {
                var originalLeft = Canvas.GetLeft(child);
                var originalTop = Canvas.GetTop(child);

                Canvas.SetLeft(child, originalLeft + 10);
                Canvas.SetTop(child, originalTop + 10);
                _root.LayoutManager.ExecuteLayoutPass();

                // Reset
                Canvas.SetLeft(child, originalLeft);
                Canvas.SetTop(child, originalTop);
                _root.LayoutManager.ExecuteLayoutPass();
            }
        }

        /// <summary>
        /// Measures moving multiple children.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Canvas_MoveManyChildren()
        {
            var movedCount = Math.Min(20, _canvas.Children.Count);
            var originals = new (double Left, double Top)[movedCount];

            // Store originals and move
            for (var i = 0; i < movedCount; i++)
            {
                if (_canvas.Children[i] is Control child)
                {
                    originals[i] = (Canvas.GetLeft(child), Canvas.GetTop(child));
                    Canvas.SetLeft(child, originals[i].Left + 10);
                    Canvas.SetTop(child, originals[i].Top + 10);
                }
            }

            _root.LayoutManager.ExecuteLayoutPass();

            // Reset
            for (var i = 0; i < movedCount; i++)
            {
                if (_canvas.Children[i] is Control child)
                {
                    Canvas.SetLeft(child, originals[i].Left);
                    Canvas.SetTop(child, originals[i].Top);
                }
            }

            _root.LayoutManager.ExecuteLayoutPass();
        }

        private static void InvalidateRecursive(Control control)
        {
            if (control is Layoutable layoutable)
            {
                SetIsMeasureValid(layoutable, false);
                SetIsArrangeValid(layoutable, false);
            }

            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    InvalidateRecursive(child);
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
