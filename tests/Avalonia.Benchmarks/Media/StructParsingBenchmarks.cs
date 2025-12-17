using System;
using Avalonia.Media.Transformation;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Media
{
    [MemoryDiagnoser]
    public class StructParsingBenchmarks
    {
        private const string PointString = "100.5, 200.5";
        private const string SizeString = "800, 600";
        private const string RectString = "0, 0, 100, 100";
        private const string ThicknessString = "5, 10, 5, 10";
        private const string CornerRadiusString = "8, 8, 0, 0";
        private const string MatrixString = "1, 0, 0, 1, 0, 0";
        private const string TransformString = "translate(100px, 50px) rotate(45deg) scale(1.5)";

        [Benchmark(Baseline = true)]
        public Point ParsePoint()
        {
            return Point.Parse(PointString);
        }

        [Benchmark]
        public Size ParseSize()
        {
            return Size.Parse(SizeString);
        }

        [Benchmark]
        public Rect ParseRect()
        {
            return Rect.Parse(RectString);
        }

        [Benchmark]
        public Thickness ParseThickness()
        {
            return Thickness.Parse(ThicknessString);
        }

        [Benchmark]
        public CornerRadius ParseCornerRadius()
        {
            return CornerRadius.Parse(CornerRadiusString);
        }

        [Benchmark]
        public Matrix ParseMatrix()
        {
            return Matrix.Parse(MatrixString);
        }

        [Benchmark]
        public TransformOperations ParseTransform()
        {
            return TransformOperations.Parse(TransformString);
        }

        [Benchmark]
        public Vector ParseVector()
        {
            return Vector.Parse(PointString);
        }

        [Benchmark]
        public PixelPoint ParsePixelPoint()
        {
            return PixelPoint.Parse("100, 200");
        }

        [Benchmark]
        public PixelSize ParsePixelSize()
        {
            return PixelSize.Parse("1920, 1080");
        }
    }
}
