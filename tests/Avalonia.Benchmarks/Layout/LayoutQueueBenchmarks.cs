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
    /// Benchmarks for LayoutQueue performance.
    /// Tests the cycle detection dictionary and queue operations.
    /// </summary>
    [MemoryDiagnoser]
    public class LayoutQueueBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private List<Control> _controls = null!;

        [Params(100, 500, 1000)]
        public int ControlCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot { Renderer = new NullRenderer() };
            var panel = new StackPanel();
            _root.Child = panel;

            _controls = new List<Control>(ControlCount);
            for (var i = 0; i < ControlCount; i++)
            {
                var button = new Button { Width = 100, Height = 30 };
                panel.Children.Add(button);
                _controls.Add(button);
            }

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        /// <summary>
        /// Measures enqueue performance with many controls.
        /// Tests dictionary allocation overhead.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EnqueueManyControls()
        {
            foreach (var control in _controls)
            {
                control.InvalidateMeasure();
            }
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of repeated enqueues (cycle detection).
        /// Tests the cycle counting mechanism.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RepeatedEnqueues()
        {
            // This simulates a scenario where controls keep invalidating
            // during the layout pass
            var subset = _controls.GetRange(0, Math.Min(10, _controls.Count));
            
            foreach (var control in subset)
            {
                control.InvalidateMeasure();
            }
            
            // Force multiple invalidations during layout would happen
            // in real cycle scenarios, but we can measure the baseline
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures layout pass with controls at different tree depths.
        /// Tests queue ordering efficiency.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MixedDepthInvalidation()
        {
            // Invalidate every Nth control to create mixed depth pattern
            for (var i = 0; i < _controls.Count; i += 5)
            {
                _controls[i].InvalidateMeasure();
            }
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Baseline: sequential single invalidations.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SequentialSingleInvalidations()
        {
            for (var i = 0; i < Math.Min(20, _controls.Count); i++)
            {
                _controls[i].InvalidateMeasure();
                _root.LayoutManager.ExecuteLayoutPass();
            }
        }

        /// <summary>
        /// Batched invalidations vs sequential.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BatchedInvalidations()
        {
            for (var i = 0; i < Math.Min(20, _controls.Count); i++)
            {
                _controls[i].InvalidateMeasure();
            }
            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
