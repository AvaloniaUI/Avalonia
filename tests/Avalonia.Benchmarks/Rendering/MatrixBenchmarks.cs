using System;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering
{
    /// <summary>
    /// Benchmarks for transform matrix operations.
    /// Tests the cost of matrix computations in the rendering pipeline.
    /// </summary>
    [MemoryDiagnoser]
    public class MatrixBenchmarks
    {
        private Matrix[] _matrices = null!;
        private Matrix _identity;
        private Matrix _translate;
        private Matrix _rotate;
        private Matrix _scale;
        private Matrix _complex;

        [Params(100, 500, 1000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _matrices = new Matrix[Count];
            _identity = Matrix.Identity;
            _translate = Matrix.CreateTranslation(100, 200);
            _rotate = Matrix.CreateRotation(Math.PI / 4);
            _scale = Matrix.CreateScale(2, 2);
            _complex = _translate * _rotate * _scale;

            var random = new Random(42);
            for (var i = 0; i < Count; i++)
            {
                _matrices[i] = Matrix.CreateTranslation(random.Next(1000), random.Next(1000))
                    * Matrix.CreateRotation(random.NextDouble() * Math.PI * 2)
                    * Matrix.CreateScale(random.NextDouble() * 2 + 0.5, random.NextDouble() * 2 + 0.5);
            }
        }

        /// <summary>
        /// Measures the cost of matrix multiplication.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MatrixMultiplication()
        {
            var result = _identity;
            for (var i = 0; i < Count; i++)
            {
                result = result * _matrices[i];
            }
        }

        /// <summary>
        /// Measures the cost of matrix inversion.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MatrixInversion()
        {
            for (var i = 0; i < Count; i++)
            {
                _ = _matrices[i].Invert();
            }
        }

        /// <summary>
        /// Measures the cost of point transformation.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TransformPoint()
        {
            var point = new Point(100, 100);
            for (var i = 0; i < Count; i++)
            {
                point = _matrices[i].Transform(point);
            }
        }

        /// <summary>
        /// Measures the cost of rect transformation.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TransformRect()
        {
            var rect = new Rect(0, 0, 100, 100);
            for (var i = 0; i < Count; i++)
            {
                rect = rect.TransformToAABB(_matrices[i]);
            }
        }

        /// <summary>
        /// Measures common matrix creation patterns.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateMatrices()
        {
            for (var i = 0; i < Count; i++)
            {
                _ = Matrix.CreateTranslation(i, i * 2);
                _ = Matrix.CreateRotation(i * 0.01);
                _ = Matrix.CreateScale(1 + i * 0.01, 1 + i * 0.01);
            }
        }

        /// <summary>
        /// Measures the cost of matrix decomposition (used for animations).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MatrixDecompose()
        {
            for (var i = 0; i < Count; i++)
            {
                _ = Matrix.TryDecomposeTransform(_matrices[i], out _);
            }
        }

        /// <summary>
        /// Measures concatenated transform building (typical visual tree traversal).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BuildTransformChain()
        {
            var stack = new Matrix[10];
            stack[0] = Matrix.Identity;
            
            for (var i = 0; i < Count; i++)
            {
                var depth = i % 10;
                if (depth == 0)
                {
                    stack[0] = _matrices[i];
                }
                else
                {
                    stack[depth] = stack[depth - 1] * _matrices[i];
                }
            }
        }
    }
}
