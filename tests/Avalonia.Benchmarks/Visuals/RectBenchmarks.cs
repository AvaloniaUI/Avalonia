using System;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals
{
    /// <summary>
    /// Benchmarks for Rect operations - these are heavily used throughout the codebase
    /// for layout calculations, hit testing, and rendering.
    /// </summary>
    [MemoryDiagnoser]
    public class RectBenchmarks
    {
        private static readonly Rect s_rect1 = new Rect(10, 20, 100, 200);
        private static readonly Rect s_rect2 = new Rect(50, 60, 80, 150);
        private static readonly Rect s_nonIntersecting = new Rect(500, 500, 50, 50);
        private static readonly Point s_pointInside = new Point(50, 100);
        private static readonly Point s_pointOutside = new Point(200, 300);
        private static readonly Thickness s_thickness = new Thickness(5, 10, 5, 10);
        private static readonly Vector s_offset = new Vector(10, 20);
        private static readonly Vector s_scale = new Vector(2, 2);
        private static readonly Matrix s_rotationMatrix = Matrix.CreateRotation(Math.PI / 4);

        [Benchmark(Baseline = true)]
        public bool ContainsPoint_Inside()
        {
            return s_rect1.Contains(s_pointInside);
        }

        [Benchmark]
        public bool ContainsPoint_Outside()
        {
            return s_rect1.Contains(s_pointOutside);
        }

        [Benchmark]
        public bool ContainsRect()
        {
            return s_rect1.Contains(s_rect2);
        }

        [Benchmark]
        public bool Intersects_True()
        {
            return s_rect1.Intersects(s_rect2);
        }

        [Benchmark]
        public bool Intersects_False()
        {
            return s_rect1.Intersects(s_nonIntersecting);
        }

        [Benchmark]
        public Rect Intersect()
        {
            return s_rect1.Intersect(s_rect2);
        }

        [Benchmark]
        public Rect Union()
        {
            return s_rect1.Union(s_rect2);
        }

        [Benchmark]
        public Rect Inflate_Thickness()
        {
            return s_rect1.Inflate(s_thickness);
        }

        [Benchmark]
        public Rect Deflate_Thickness()
        {
            return s_rect1.Deflate(s_thickness);
        }

        [Benchmark]
        public Rect Translate()
        {
            return s_rect1.Translate(s_offset);
        }

        [Benchmark]
        public Rect Scale()
        {
            return s_rect1 * s_scale;
        }

        [Benchmark]
        public Rect TransformToAABB()
        {
            return s_rect1.TransformToAABB(s_rotationMatrix);
        }

        [Benchmark]
        public Rect CenterRect()
        {
            return s_rect1.CenterRect(s_rect2);
        }

        [Benchmark]
        public bool Equals_Same()
        {
            return s_rect1.Equals(s_rect1);
        }

        [Benchmark]
        public bool Equals_Different()
        {
            return s_rect1.Equals(s_rect2);
        }

        [Benchmark]
        public int GetHashCode_Rect()
        {
            return s_rect1.GetHashCode();
        }

        [Benchmark]
        public Point GetCenter()
        {
            return s_rect1.Center;
        }

        [Benchmark]
        public Point GetTopLeft()
        {
            return s_rect1.TopLeft;
        }

        [Benchmark]
        public Point GetBottomRight()
        {
            return s_rect1.BottomRight;
        }
    }
}
