using System;
using System.Runtime.CompilerServices;
using Avalonia.Collections.Pooled;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering
{
    /// <summary>
    /// Benchmarks for DirtyRectTracker performance.
    /// Tests dirty rect union operations and intersection checks.
    /// </summary>
    [MemoryDiagnoser]
    public class DirtyRectBenchmarks
    {
        private DirtyRectTracker _tracker = null!;
        private LtrbPixelRect[] _rects = null!;
        private LtrbRect[] _checkRects = null!;

        [Params(10, 50, 100)]
        public int RectCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _tracker = new DirtyRectTracker();
            _rects = new LtrbPixelRect[RectCount];
            _checkRects = new LtrbRect[RectCount];
            
            var random = new Random(42);
            for (var i = 0; i < RectCount; i++)
            {
                var x = random.Next(0, 800);
                var y = random.Next(0, 600);
                var w = random.Next(10, 100);
                var h = random.Next(10, 100);
                _rects[i] = new LtrbPixelRect(x, y, x + w, y + h);
                _checkRects[i] = new LtrbRect(x, y, x + w, y + h);
            }
        }

        /// <summary>
        /// Measures the cost of adding dirty rects.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddDirtyRects()
        {
            _tracker.Reset();
            for (var i = 0; i < _rects.Length; i++)
            {
                _tracker.AddRect(_rects[i]);
            }
        }

        /// <summary>
        /// Measures the cost of intersection checks.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void IntersectionChecks()
        {
            _tracker.Reset();
            _tracker.AddRect(new LtrbPixelRect(0, 0, 800, 600));
            
            var count = 0;
            for (var i = 0; i < _checkRects.Length; i++)
            {
                if (_tracker.Intersects(_checkRects[i]))
                    count++;
            }
        }

        /// <summary>
        /// Measures the combined workflow of adding and checking.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddAndCheck()
        {
            _tracker.Reset();
            
            // Add half the rects
            for (var i = 0; i < _rects.Length / 2; i++)
            {
                _tracker.AddRect(_rects[i]);
            }
            
            // Check the other half
            var count = 0;
            for (var i = _rects.Length / 2; i < _rects.Length; i++)
            {
                if (_tracker.Intersects(_checkRects[i]))
                    count++;
            }
        }
    }
}
