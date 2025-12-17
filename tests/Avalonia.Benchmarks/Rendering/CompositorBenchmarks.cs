using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering
{
    /// <summary>
    /// Benchmarks for Compositor and rendering pipeline performance.
    /// Tests batch serialization, visual updates, and dirty rect tracking.
    /// </summary>
    [MemoryDiagnoser]
    public class CompositorBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private List<Control> _controls = null!;

        [Params(10, 50, 100)]
        public int ControlCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot(true, null) { Renderer = new NullRenderer() };
            var panel = new Canvas { Width = 800, Height = 600 };
            _root.Child = panel;

            _controls = new List<Control>(ControlCount);
            for (var i = 0; i < ControlCount; i++)
            {
                var rect = new Rectangle
                {
                    Width = 50,
                    Height = 50,
                    Fill = Brushes.Blue
                };
                Canvas.SetLeft(rect, (i % 10) * 60);
                Canvas.SetTop(rect, (i / 10) * 60);
                panel.Children.Add(rect);
                _controls.Add(rect);
            }

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        /// <summary>
        /// Measures the cost of rendering the visual tree.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RenderVisualTree()
        {
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        /// <summary>
        /// Measures the cost of invalidating and re-rendering a single control.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InvalidateSingleControl()
        {
            _controls[0].InvalidateVisual();
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        /// <summary>
        /// Measures the cost of invalidating and re-rendering all controls.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InvalidateAllControls()
        {
            foreach (var control in _controls)
            {
                control.InvalidateVisual();
            }
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        /// <summary>
        /// Measures the cost of changing visual properties that trigger compositor updates.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ChangeOpacity()
        {
            foreach (var control in _controls)
            {
                control.Opacity = control.Opacity > 0.5 ? 0.3 : 0.7;
            }
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        /// <summary>
        /// Measures the cost of changing transforms on controls (creates new transform objects).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ChangeTransforms()
        {
            for (var i = 0; i < _controls.Count; i++)
            {
                var control = _controls[i];
                var angle = (i * 10) % 360;
                control.RenderTransform = new RotateTransform(angle);
            }
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        /// <summary>
        /// Measures the cost of updating existing transforms on controls (reuses transform objects).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UpdateExistingTransforms()
        {
            for (var i = 0; i < _controls.Count; i++)
            {
                var control = _controls[i];
                if (control.RenderTransform is RotateTransform rotate)
                {
                    rotate.Angle = (rotate.Angle + 10) % 360;
                }
                else
                {
                    control.RenderTransform = new RotateTransform((i * 10) % 360);
                }
            }
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        /// <summary>
        /// Measures the cost of using pooled transforms via TransformPool.SetRotation.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PooledTransforms()
        {
            for (var i = 0; i < _controls.Count; i++)
            {
                var control = _controls[i];
                var angle = (i * 10 + 5) % 360;
                control.RenderTransform = TransformPool.SetRotation(control.RenderTransform as Transform, angle);
            }
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
        }

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
