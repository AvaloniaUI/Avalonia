using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for EffectiveViewport calculations.
    /// Tests viewport calculation performance with different tree depths and transforms.
    /// </summary>
    [MemoryDiagnoser]
    public class EffectiveViewportBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _flatRoot = null!;
        private TestRoot _deepRoot = null!;
        private TestRoot _transformedRoot = null!;
        private List<Control> _flatControls = null!;
        private List<Control> _deepControls = null!;
        private List<Control> _transformedControls = null!;

        [Params(5, 20, 50)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Flat hierarchy with viewport listeners
            _flatRoot = new TestRoot { Renderer = new NullRenderer() };
            (_flatRoot.Child, _flatControls) = CreateFlatHierarchy(ListenerCount);
            _flatRoot.LayoutManager.ExecuteInitialLayoutPass();
            RegisterViewportListeners(_flatControls);

            // Deep hierarchy with viewport listeners at various depths
            _deepRoot = new TestRoot { Renderer = new NullRenderer() };
            (_deepRoot.Child, _deepControls) = CreateDeepHierarchy(ListenerCount);
            _deepRoot.LayoutManager.ExecuteInitialLayoutPass();
            RegisterViewportListeners(_deepControls);

            // Hierarchy with render transforms
            _transformedRoot = new TestRoot { Renderer = new NullRenderer() };
            (_transformedRoot.Child, _transformedControls) = CreateTransformedHierarchy(ListenerCount);
            _transformedRoot.LayoutManager.ExecuteInitialLayoutPass();
            RegisterViewportListeners(_transformedControls);
        }

        private static (Control, List<Control>) CreateFlatHierarchy(int count)
        {
            var panel = new StackPanel();
            var controls = new List<Control>();

            for (var i = 0; i < count; i++)
            {
                var button = new Button { Width = 100, Height = 30, Content = $"B{i}" };
                panel.Children.Add(button);
                controls.Add(button);
            }

            return (panel, controls);
        }

        private static (Control, List<Control>) CreateDeepHierarchy(int count)
        {
            var controls = new List<Control>();
            var root = new StackPanel();
            var depth = 10;
            var controlsPerDepth = Math.Max(1, count / depth);

            Panel current = root;
            for (var d = 0; d < depth; d++)
            {
                for (var i = 0; i < controlsPerDepth && controls.Count < count; i++)
                {
                    var button = new Button { Width = 100, Height = 30, Content = $"D{d}-B{i}" };
                    current.Children.Add(button);
                    controls.Add(button);
                }

                if (d < depth - 1)
                {
                    var next = new StackPanel();
                    current.Children.Add(next);
                    current = next;
                }
            }

            return (root, controls);
        }

        private static (Control, List<Control>) CreateTransformedHierarchy(int count)
        {
            var controls = new List<Control>();
            var root = new StackPanel();

            for (var i = 0; i < count; i++)
            {
                var wrapper = new Border
                {
                    RenderTransform = new RotateTransform(i * 5),
                    RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
                };

                var button = new Button { Width = 100, Height = 30, Content = $"T{i}" };
                wrapper.Child = button;
                root.Children.Add(wrapper);
                controls.Add(button);
            }

            return (root, controls);
        }

        private static void RegisterViewportListeners(List<Control> controls)
        {
            foreach (var control in controls)
            {
                if (control is Layoutable layoutable)
                {
                    layoutable.EffectiveViewportChanged += OnViewportChanged;
                }
            }
        }

        private static void OnViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            // Empty handler - we're just measuring the cost of raising the event
        }

        /// <summary>
        /// Layout pass with flat hierarchy viewport listeners.
        /// Baseline for viewport calculation.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FlatHierarchy_LayoutWithViewport()
        {
            InvalidateAll(_flatControls);
            _flatRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Layout pass with deep hierarchy viewport listeners.
        /// Tests recursive viewport calculation cost.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DeepHierarchy_LayoutWithViewport()
        {
            InvalidateAll(_deepControls);
            _deepRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Layout pass with transformed hierarchy viewport listeners.
        /// Tests matrix operations in viewport calculation.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TransformedHierarchy_LayoutWithViewport()
        {
            InvalidateAll(_transformedControls);
            _transformedRoot.LayoutManager.ExecuteLayoutPass();
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

        [GlobalCleanup]
        public void Cleanup()
        {
            // Unregister viewport listeners
            foreach (var control in _flatControls)
            {
                if (control is Layoutable layoutable)
                    layoutable.EffectiveViewportChanged -= OnViewportChanged;
            }
            foreach (var control in _deepControls)
            {
                if (control is Layoutable layoutable)
                    layoutable.EffectiveViewportChanged -= OnViewportChanged;
            }
            foreach (var control in _transformedControls)
            {
                if (control is Layoutable layoutable)
                    layoutable.EffectiveViewportChanged -= OnViewportChanged;
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
