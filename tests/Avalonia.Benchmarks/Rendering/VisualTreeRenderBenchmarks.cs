using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering
{
    /// <summary>
    /// Benchmarks for visual tree rendering scenarios.
    /// Tests different visual tree structures and their rendering costs.
    /// </summary>
    [MemoryDiagnoser]
    public class VisualTreeRenderBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _flatRoot = null!;
        private TestRoot _deepRoot = null!;
        private TestRoot _wideRoot = null!;

        [Params(100)]
        public int ControlCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Setup flat tree (all controls in one panel)
            _flatRoot = new TestRoot(true, null) { Renderer = new NullRenderer() };
            var flatPanel = new Canvas { Width = 1000, Height = 1000 };
            _flatRoot.Child = flatPanel;
            for (var i = 0; i < ControlCount; i++)
            {
                var rect = new Rectangle { Width = 50, Height = 50, Fill = Brushes.Blue };
                Canvas.SetLeft(rect, (i % 20) * 50);
                Canvas.SetTop(rect, (i / 20) * 50);
                flatPanel.Children.Add(rect);
            }
            _flatRoot.LayoutManager.ExecuteInitialLayoutPass();

            // Setup deep tree (nested panels)
            _deepRoot = new TestRoot(true, null) { Renderer = new NullRenderer() };
            Panel current = new StackPanel();
            _deepRoot.Child = current;
            for (var i = 0; i < ControlCount; i++)
            {
                var rect = new Rectangle { Width = 50, Height = 50, Fill = Brushes.Red };
                current.Children.Add(rect);
                if (i < ControlCount - 1 && i % 10 == 0)
                {
                    var newPanel = new StackPanel();
                    current.Children.Add(newPanel);
                    current = newPanel;
                }
            }
            _deepRoot.LayoutManager.ExecuteInitialLayoutPass();

            // Setup wide tree (multiple sibling panels)
            _wideRoot = new TestRoot(true, null) { Renderer = new NullRenderer() };
            var widePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
            _wideRoot.Child = widePanel;
            var perGroup = ControlCount / 10;
            for (var g = 0; g < 10; g++)
            {
                var group = new StackPanel();
                for (var i = 0; i < perGroup; i++)
                {
                    var rect = new Rectangle { Width = 50, Height = 50, Fill = Brushes.Green };
                    group.Children.Add(rect);
                }
                widePanel.Children.Add(group);
            }
            _wideRoot.LayoutManager.ExecuteInitialLayoutPass();
        }

        /// <summary>
        /// Measures rendering of a flat visual tree.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RenderFlatTree()
        {
            _flatRoot.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        /// <summary>
        /// Measures rendering of a deep visual tree.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RenderDeepTree()
        {
            _deepRoot.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        /// <summary>
        /// Measures rendering of a wide visual tree.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RenderWideTree()
        {
            _wideRoot.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        /// <summary>
        /// Measures partial invalidation in flat tree.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InvalidatePartialFlatTree()
        {
            var panel = (Canvas)_flatRoot.Child!;
            // Invalidate every 5th control
            for (var i = 0; i < panel.Children.Count; i += 5)
            {
                panel.Children[i].InvalidateVisual();
            }
            _flatRoot.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
