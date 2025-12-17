using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals
{
    [MemoryDiagnoser]
    public class MatrixBenchmarks
    {
        private static readonly Matrix s_identity = Matrix.Identity;
        private static readonly Matrix s_rotation = Matrix.CreateRotation(0.5);
        private static readonly Matrix s_scale = Matrix.CreateScale(2.0, 3.0);
        private static readonly Matrix s_translation = Matrix.CreateTranslation(100, 200);
        private static readonly Matrix s_complex = s_rotation * s_scale * s_translation;
        private static readonly Point s_point = new Point(50, 75);
        private static readonly Vector s_vector = new Vector(10, 20);

        [Benchmark(Baseline = true)]
        public bool Decompose()
        {
            return Matrix.TryDecomposeTransform(s_identity, out _);
        }

        [Benchmark]
        public bool DecomposeComplex()
        {
            return Matrix.TryDecomposeTransform(s_complex, out _);
        }

        [Benchmark]
        public Matrix MultiplyMatrices()
        {
            return s_rotation * s_scale;
        }

        [Benchmark]
        public Matrix MultiplyThreeMatrices()
        {
            return s_rotation * s_scale * s_translation;
        }

        [Benchmark]
        public Point TransformPoint()
        {
            return s_complex.Transform(s_point);
        }

        [Benchmark]
        public Vector TransformVector()
        {
            return (Vector)s_complex.Transform((Point)s_vector);
        }

        [Benchmark]
        public Matrix Invert()
        {
            return s_complex.Invert();
        }

        [Benchmark]
        public Matrix CreateRotation()
        {
            return Matrix.CreateRotation(0.785398); // 45 degrees
        }

        [Benchmark]
        public Matrix CreateScale()
        {
            return Matrix.CreateScale(2.0, 3.0);
        }

        [Benchmark]
        public Matrix CreateTranslation()
        {
            return Matrix.CreateTranslation(100, 200);
        }

        [Benchmark]
        public Matrix CreateSkew()
        {
            return Matrix.CreateSkew(0.1, 0.2);
        }

        [Benchmark]
        public bool IsIdentity()
        {
            return s_identity.IsIdentity;
        }

        [Benchmark]
        public bool HasInverse()
        {
            return s_complex.HasInverse;
        }
    }
}
