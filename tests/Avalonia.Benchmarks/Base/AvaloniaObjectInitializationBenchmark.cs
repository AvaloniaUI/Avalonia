using Avalonia.Controls;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class AvaloniaObjectInitializationBenchmark
    {
        [Benchmark]
        public Button InitializeButton()
        {
            return new Button();
        }
    }
}
