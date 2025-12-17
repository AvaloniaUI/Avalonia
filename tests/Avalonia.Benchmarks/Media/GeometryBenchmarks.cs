using System;
using Avalonia.Media;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Media
{
    [MemoryDiagnoser]
    public class GeometryBenchmarks
    {
        private RectangleGeometry? _rectangleGeometry;
        private EllipseGeometry? _ellipseGeometry;
        private PathGeometry? _pathGeometry;
        private StreamGeometry? _streamGeometry;
        private Point _testPoint;

        [GlobalSetup]
        public void Setup()
        {
            _rectangleGeometry = new RectangleGeometry(new Rect(0, 0, 100, 100));
            _ellipseGeometry = new EllipseGeometry(new Rect(0, 0, 100, 100));
            
            // Create a simple path geometry
            _pathGeometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true };
            figure.Segments!.Add(new LineSegment { Point = new Point(100, 0) });
            figure.Segments.Add(new LineSegment { Point = new Point(100, 100) });
            figure.Segments.Add(new LineSegment { Point = new Point(0, 100) });
            _pathGeometry.Figures!.Add(figure);

            // Create a stream geometry
            _streamGeometry = new StreamGeometry();
            using (var ctx = _streamGeometry.Open())
            {
                ctx.BeginFigure(new Point(0, 0), true);
                ctx.LineTo(new Point(100, 0));
                ctx.LineTo(new Point(100, 100));
                ctx.LineTo(new Point(0, 100));
                ctx.EndFigure(true);
            }

            _testPoint = new Point(50, 50);
        }

        /// <summary>
        /// Benchmark getting bounds of rectangle geometry
        /// </summary>
        [Benchmark(Baseline = true)]
        public Rect RectangleGetBounds()
        {
            return _rectangleGeometry!.Bounds;
        }

        /// <summary>
        /// Benchmark getting bounds of ellipse geometry
        /// </summary>
        [Benchmark]
        public Rect EllipseGetBounds()
        {
            return _ellipseGeometry!.Bounds;
        }

        /// <summary>
        /// Benchmark getting bounds of path geometry
        /// </summary>
        [Benchmark]
        public Rect PathGetBounds()
        {
            return _pathGeometry!.Bounds;
        }

        /// <summary>
        /// Benchmark getting bounds of stream geometry
        /// </summary>
        [Benchmark]
        public Rect StreamGetBounds()
        {
            return _streamGeometry!.Bounds;
        }

        /// <summary>
        /// Benchmark fill contains point for rectangle
        /// </summary>
        [Benchmark]
        public bool RectangleFillContains()
        {
            return _rectangleGeometry!.FillContains(_testPoint);
        }

        /// <summary>
        /// Benchmark fill contains point for ellipse
        /// </summary>
        [Benchmark]
        public bool EllipseFillContains()
        {
            return _ellipseGeometry!.FillContains(_testPoint);
        }

        /// <summary>
        /// Benchmark fill contains point for path
        /// </summary>
        [Benchmark]
        public bool PathFillContains()
        {
            return _pathGeometry!.FillContains(_testPoint);
        }

        /// <summary>
        /// Benchmark stream geometry creation
        /// </summary>
        [Benchmark]
        public StreamGeometry CreateStreamGeometry()
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(0, 0), true);
                ctx.LineTo(new Point(100, 0));
                ctx.LineTo(new Point(100, 100));
                ctx.LineTo(new Point(0, 100));
                ctx.EndFigure(true);
            }
            return geometry;
        }

        /// <summary>
        /// Benchmark parsing path geometry from string
        /// </summary>
        [Benchmark]
        public Geometry ParsePathGeometry()
        {
            return PathGeometry.Parse("M 0,0 L 100,0 L 100,100 L 0,100 Z");
        }

        /// <summary>
        /// Benchmark combined geometry intersection
        /// </summary>
        [Benchmark]
        public CombinedGeometry CreateCombinedGeometry()
        {
            return new CombinedGeometry(
                GeometryCombineMode.Intersect,
                _rectangleGeometry,
                _ellipseGeometry);
        }
    }
}
