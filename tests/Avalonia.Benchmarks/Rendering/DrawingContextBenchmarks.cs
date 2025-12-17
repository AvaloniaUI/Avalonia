using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering
{
    /// <summary>
    /// Benchmarks for DrawingContext operations.
    /// Tests the cost of common drawing operations.
    /// </summary>
    [MemoryDiagnoser]
    public class DrawingContextBenchmarks : IDisposable
    {
        private DrawingContext _drawingContext = null!;
        private SolidColorBrush _brush = null!;
        private Pen _pen = null!;
        private RectangleGeometry _geometry = null!;

        [GlobalSetup]
        public void Setup()
        {
            AvaloniaLocator.CurrentMutable.Bind<IPlatformRenderInterface>()
                .ToConstant(new HeadlessPlatformRenderInterface());
            
            _brush = new SolidColorBrush(Colors.Blue);
            _pen = new Pen(Brushes.Black, 1);
            _geometry = new RectangleGeometry(new Rect(0, 0, 100, 100));
            
            _drawingContext = new PlatformDrawingContext(
                new HeadlessPlatformRenderInterface.HeadlessDrawingContextStub(), true);
        }

        /// <summary>
        /// Measures the cost of drawing rectangles.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawRectangle()
        {
            for (var i = 0; i < 100; i++)
            {
                _drawingContext.DrawRectangle(_brush, _pen, new Rect(i * 10, 0, 50, 50));
            }
        }

        /// <summary>
        /// Measures the cost of drawing ellipses.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawEllipse()
        {
            for (var i = 0; i < 100; i++)
            {
                _drawingContext.DrawEllipse(_brush, _pen, new Rect(i * 10, 0, 50, 50));
            }
        }

        /// <summary>
        /// Measures the cost of drawing lines.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawLine()
        {
            for (var i = 0; i < 100; i++)
            {
                _drawingContext.DrawLine(_pen, new Point(i * 10, 0), new Point(i * 10 + 50, 50));
            }
        }

        /// <summary>
        /// Measures the cost of drawing geometry.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawGeometry()
        {
            for (var i = 0; i < 100; i++)
            {
                _drawingContext.DrawGeometry(_brush, _pen, _geometry);
            }
        }

        /// <summary>
        /// Measures the cost of push/pop clip operations.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PushPopClip()
        {
            for (var i = 0; i < 100; i++)
            {
                using (_drawingContext.PushClip(new Rect(0, 0, 100, 100)))
                {
                    _drawingContext.DrawRectangle(_brush, null, new Rect(10, 10, 80, 80));
                }
            }
        }

        /// <summary>
        /// Measures the cost of push/pop opacity operations.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PushPopOpacity()
        {
            for (var i = 0; i < 100; i++)
            {
                using (_drawingContext.PushOpacity(0.5))
                {
                    _drawingContext.DrawRectangle(_brush, null, new Rect(10, 10, 80, 80));
                }
            }
        }

        /// <summary>
        /// Measures the cost of push/pop transform operations.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PushPopTransform()
        {
            for (var i = 0; i < 100; i++)
            {
                using (_drawingContext.PushTransform(Matrix.CreateTranslation(i, 0)))
                {
                    _drawingContext.DrawRectangle(_brush, null, new Rect(10, 10, 80, 80));
                }
            }
        }

        /// <summary>
        /// Measures nested operations typical of complex visual trees.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NestedOperations()
        {
            for (var i = 0; i < 100; i++)
            {
                using (_drawingContext.PushTransform(Matrix.CreateTranslation(i * 10, 0)))
                using (_drawingContext.PushClip(new Rect(0, 0, 100, 100)))
                using (_drawingContext.PushOpacity(0.8))
                {
                    _drawingContext.DrawRectangle(_brush, _pen, new Rect(10, 10, 80, 80));
                }
            }
        }

        public void Dispose()
        {
            _drawingContext?.Dispose();
        }
    }
}
