using System;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals
{
    /// <summary>
    /// Benchmarks for Point and Vector operations - heavily used in layout, hit testing,
    /// input handling, and rendering calculations.
    /// </summary>
    [MemoryDiagnoser]
    public class PointVectorBenchmarks
    {
        private static readonly Point s_point1 = new Point(100, 200);
        private static readonly Point s_point2 = new Point(50, 75);
        private static readonly Vector s_vector1 = new Vector(10, 20);
        private static readonly Vector s_vector2 = new Vector(5, 15);
        private static readonly Matrix s_matrix = Matrix.CreateRotation(Math.PI / 4);
        private const double s_scalar = 2.5;

        // Point operations
        [Benchmark(Baseline = true)]
        public Point Point_Add_Vector()
        {
            return s_point1 + s_vector1;
        }

        [Benchmark]
        public Point Point_Add_Point()
        {
            return s_point1 + s_point2;
        }

        [Benchmark]
        public Point Point_Subtract_Vector()
        {
            return s_point1 - s_vector1;
        }

        [Benchmark]
        public Point Point_Subtract_Point()
        {
            return s_point1 - s_point2;
        }

        [Benchmark]
        public Point Point_Multiply_Scalar()
        {
            return s_point1 * s_scalar;
        }

        [Benchmark]
        public Point Point_Divide_Scalar()
        {
            return s_point1 / s_scalar;
        }

        [Benchmark]
        public Point Point_Transform()
        {
            return s_point1.Transform(s_matrix);
        }

        [Benchmark]
        public double Point_Distance()
        {
            return Point.Distance(s_point1, s_point2);
        }

        [Benchmark]
        public bool Point_Equals()
        {
            return s_point1.Equals(s_point2);
        }

        [Benchmark]
        public bool Point_NearlyEquals()
        {
            return s_point1.NearlyEquals(s_point2);
        }

        [Benchmark]
        public int Point_GetHashCode()
        {
            return s_point1.GetHashCode();
        }

        [Benchmark]
        public Point Point_WithX()
        {
            return s_point1.WithX(300);
        }

        [Benchmark]
        public Point Point_WithY()
        {
            return s_point1.WithY(400);
        }

        // Vector operations
        [Benchmark]
        public Vector Vector_Add()
        {
            return s_vector1 + s_vector2;
        }

        [Benchmark]
        public Vector Vector_Subtract()
        {
            return s_vector1 - s_vector2;
        }

        [Benchmark]
        public Vector Vector_Multiply_Scalar()
        {
            return s_vector1 * s_scalar;
        }

        [Benchmark]
        public Vector Vector_Divide_Scalar()
        {
            return s_vector1 / s_scalar;
        }

        [Benchmark]
        public Vector Vector_Negate()
        {
            return -s_vector1;
        }

        [Benchmark]
        public double Vector_Length()
        {
            return s_vector1.Length;
        }

        [Benchmark]
        public double Vector_SquaredLength()
        {
            return s_vector1.SquaredLength;
        }

        [Benchmark]
        public Vector Vector_Normalize()
        {
            return s_vector1.Normalize();
        }

        [Benchmark]
        public double Vector_Dot()
        {
            return Vector.Dot(s_vector1, s_vector2);
        }

        [Benchmark]
        public double Vector_Cross()
        {
            return Vector.Cross(s_vector1, s_vector2);
        }

        [Benchmark]
        public bool Vector_Equals()
        {
            return s_vector1.Equals(s_vector2);
        }

        [Benchmark]
        public bool Vector_NearlyEquals()
        {
            return s_vector1.NearlyEquals(s_vector2);
        }
    }
}
