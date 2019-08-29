using Avalonia.Controls;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class AvaloniaObjectInitializationBenchmark
    {
        [Benchmark(OperationsPerInvoke = 1000)]
        public Button InitializeButton()
        {
            return new Button();
        }
    }
}
