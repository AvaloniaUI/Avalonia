using System;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals
{
    /// <summary>
    /// Benchmarks for RelativePoint and RelativeRect operations.
    /// These are used for gradients, transforms, and other relative positioning.
    /// </summary>
    [MemoryDiagnoser]
    public class RelativePointRectBenchmarks
    {
        private RelativePoint _relativePointAbsolute;
        private RelativePoint _relativePointRelative;
        private RelativeRect _relativeRectAbsolute;
        private RelativeRect _relativeRectRelative;
        private Size _containerSize;
        private Rect _boundingBox;

        // Parsing strings
        private const string AbsolutePointString = "100, 200";
        private const string RelativePointString = "50%, 50%";
        private const string AbsoluteRectString = "10, 20, 300, 400";
        private const string RelativeRectString = "0%, 0%, 100%, 100%";

        [GlobalSetup]
        public void Setup()
        {
            _relativePointAbsolute = new RelativePoint(100, 200, RelativeUnit.Absolute);
            _relativePointRelative = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            _relativeRectAbsolute = new RelativeRect(10, 20, 300, 400, RelativeUnit.Absolute);
            _relativeRectRelative = RelativeRect.Fill;
            _containerSize = new Size(800, 600);
            _boundingBox = new Rect(50, 50, 700, 500);
        }

        #region RelativePoint Operations

        [Benchmark]
        public Point RelativePoint_ToPixels_Absolute()
        {
            return _relativePointAbsolute.ToPixels(_containerSize);
        }

        [Benchmark]
        public Point RelativePoint_ToPixels_Relative()
        {
            return _relativePointRelative.ToPixels(_containerSize);
        }

        [Benchmark]
        public Point RelativePoint_ToPixels_Absolute_WithRect()
        {
            return _relativePointAbsolute.ToPixels(_boundingBox);
        }

        [Benchmark]
        public Point RelativePoint_ToPixels_Relative_WithRect()
        {
            return _relativePointRelative.ToPixels(_boundingBox);
        }

        [Benchmark]
        public bool RelativePoint_Equals()
        {
            return _relativePointAbsolute == _relativePointRelative;
        }

        [Benchmark]
        public int RelativePoint_GetHashCode()
        {
            return _relativePointAbsolute.GetHashCode();
        }

        [Benchmark(Baseline = true)]
        public RelativePoint RelativePoint_Parse_Absolute()
        {
            return RelativePoint.Parse(AbsolutePointString);
        }

        [Benchmark]
        public RelativePoint RelativePoint_Parse_Relative()
        {
            return RelativePoint.Parse(RelativePointString);
        }

        [Benchmark]
        public string RelativePoint_ToString_Absolute()
        {
            return _relativePointAbsolute.ToString();
        }

        [Benchmark]
        public string RelativePoint_ToString_Relative()
        {
            return _relativePointRelative.ToString();
        }

        #endregion

        #region RelativeRect Operations

        [Benchmark]
        public Rect RelativeRect_ToPixels_Absolute()
        {
            return _relativeRectAbsolute.ToPixels(_containerSize);
        }

        [Benchmark]
        public Rect RelativeRect_ToPixels_Relative()
        {
            return _relativeRectRelative.ToPixels(_containerSize);
        }

        [Benchmark]
        public Rect RelativeRect_ToPixels_Absolute_WithRect()
        {
            return _relativeRectAbsolute.ToPixels(_boundingBox);
        }

        [Benchmark]
        public Rect RelativeRect_ToPixels_Relative_WithRect()
        {
            return _relativeRectRelative.ToPixels(_boundingBox);
        }

        [Benchmark]
        public bool RelativeRect_Equals()
        {
            return _relativeRectAbsolute == _relativeRectRelative;
        }

        [Benchmark]
        public int RelativeRect_GetHashCode()
        {
            return _relativeRectAbsolute.GetHashCode();
        }

        [Benchmark]
        public RelativeRect RelativeRect_Parse_Absolute()
        {
            return RelativeRect.Parse(AbsoluteRectString);
        }

        [Benchmark]
        public RelativeRect RelativeRect_Parse_Relative()
        {
            return RelativeRect.Parse(RelativeRectString);
        }

        #endregion

        #region RoundedRect Operations

        private RoundedRect _roundedRect;
        private RoundedRect _uniformRoundedRect;
        private Point _pointInside;
        private Point _pointOutside;
        private Point _pointInCorner;

        [GlobalSetup(Target = nameof(RoundedRect_ContainsExclusive_Inside))]
        public void SetupRoundedRect()
        {
            _roundedRect = new RoundedRect(new Rect(0, 0, 100, 100), 10, 10, 5, 5);
            _uniformRoundedRect = new RoundedRect(new Rect(0, 0, 100, 100), 10);
            _pointInside = new Point(50, 50);
            _pointOutside = new Point(150, 150);
            _pointInCorner = new Point(2, 2);
        }

        [Benchmark]
        public bool RoundedRect_ContainsExclusive_Inside()
        {
            return _roundedRect.ContainsExclusive(_pointInside);
        }

        [Benchmark]
        public bool RoundedRect_ContainsExclusive_Outside()
        {
            return _roundedRect.ContainsExclusive(_pointOutside);
        }

        [Benchmark]
        public bool RoundedRect_ContainsExclusive_Corner()
        {
            return _roundedRect.ContainsExclusive(_pointInCorner);
        }

        [Benchmark]
        public bool RoundedRect_IsRounded()
        {
            return _roundedRect.IsRounded;
        }

        [Benchmark]
        public bool RoundedRect_IsUniform()
        {
            return _uniformRoundedRect.IsUniform;
        }

        [Benchmark]
        public RoundedRect RoundedRect_Inflate()
        {
            return _roundedRect.Inflate(5, 5);
        }

        [Benchmark]
        public RoundedRect RoundedRect_Deflate()
        {
            return _roundedRect.Deflate(5, 5);
        }

        [Benchmark]
        public bool RoundedRect_Equals()
        {
            return _roundedRect == _uniformRoundedRect;
        }

        [Benchmark]
        public int RoundedRect_GetHashCode()
        {
            return _roundedRect.GetHashCode();
        }

        [Benchmark]
        public RoundedRect RoundedRect_FromRect()
        {
            return new RoundedRect(new Rect(0, 0, 100, 100));
        }

        [Benchmark]
        public RoundedRect RoundedRect_FromRectAndRadius()
        {
            return new RoundedRect(new Rect(0, 0, 100, 100), 10);
        }

        [Benchmark]
        public RoundedRect RoundedRect_FromRectAndCornerRadius()
        {
            return new RoundedRect(new Rect(0, 0, 100, 100), new CornerRadius(10, 10, 5, 5));
        }

        #endregion
    }
}
