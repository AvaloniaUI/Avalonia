using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals
{
    [MemoryDiagnoser, InProcess]
    public class MatrixBenchmarks
    {
        private static readonly Matrix s_data = Matrix.Identity;

        [Benchmark(Baseline = true)]
        public bool Decompose()
        {
            return Matrix.TryDecomposeTransform(s_data, out Matrix.Decomposed decomposed);
        }
    }
}
