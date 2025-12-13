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
    /// Benchmarks for layout invalidation patterns.
    /// Tests the cost of invalidation propagation and batching opportunities.
    /// </summary>
    [MemoryDiagnoser]
    public class InvalidationBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private List<Control> _controls = null!;
        private Button _singleControl = null!;
        private StackPanel _panelWithManyChildren = null!;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot { Renderer = new NullRenderer() };

            var mainPanel = new StackPanel();
            _root.Child = mainPanel;

            _controls = new List<Control>();
            _panelWithManyChildren = new StackPanel();
            mainPanel.Children.Add(_panelWithManyChildren);

            // Create 100 children in the panel
            for (var i = 0; i < 100; i++)
            {
                var button = new Button { Width = 100, Height = 30, Content = $"Button {i}" };
                _panelWithManyChildren.Children.Add(button);
                _controls.Add(button);
            }

            _singleControl = new Button { Width = 100, Height = 30 };
            mainPanel.Children.Add(_singleControl);
            _controls.Add(_singleControl);

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        /// <summary>
        /// Measures the cost of InvalidateMeasure on a single control.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SingleInvalidateMeasure()
        {
            _singleControl.InvalidateMeasure();
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of InvalidateArrange on a single control.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SingleInvalidateArrange()
        {
            _singleControl.InvalidateArrange();
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of multiple sequential property changes (no batching).
        /// This is the scenario that could benefit from batched invalidation.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MultiplePropertyChanges_Sequential()
        {
            // Each of these triggers InvalidateMeasure
            _singleControl.Width = 110;
            _singleControl.Height = 35;
            _singleControl.Margin = new Thickness(5);
            _singleControl.MinWidth = 50;
            _singleControl.MaxWidth = 200;

            _root.LayoutManager.ExecuteLayoutPass();

            // Reset
            _singleControl.Width = 100;
            _singleControl.Height = 30;
            _singleControl.Margin = new Thickness(0);
            _singleControl.MinWidth = 0;
            _singleControl.MaxWidth = double.PositiveInfinity;

            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of invalidating all children in a panel.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InvalidateAllChildren()
        {
            foreach (var child in _panelWithManyChildren.Children)
            {
                child.InvalidateMeasure();
            }

            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of invalidating parent when children change.
        /// Tests ChildDesiredSizeChanged propagation.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ChildDesiredSizeChanged_Propagation()
        {
            // Change size of first few children
            for (var i = 0; i < 10; i++)
            {
                if (_panelWithManyChildren.Children[i] is Button button)
                {
                    button.Width = 120;
                }
            }

            _root.LayoutManager.ExecuteLayoutPass();

            // Reset
            for (var i = 0; i < 10; i++)
            {
                if (_panelWithManyChildren.Children[i] is Button button)
                {
                    button.Width = 100;
                }
            }

            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of invalidation through deep nesting.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DeepNestingInvalidation()
        {
            // Create a deep nesting scenario if not already there
            var current = _panelWithManyChildren;
            for (var depth = 0; depth < 10; depth++)
            {
                if (current.Children.Count > 0 && current.Children[0] is StackPanel nested)
                {
                    current = nested;
                }
            }

            // Invalidate deepest
            current.InvalidateMeasure();
            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
