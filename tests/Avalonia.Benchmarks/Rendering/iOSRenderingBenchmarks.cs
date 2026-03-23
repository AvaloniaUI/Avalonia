#nullable enable
using System;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering
{
    /// <summary>
    /// Benchmarks for Avalonia.iOS hot path patterns.
    /// Run with: dotnet run -c Release -- --filter *iOSRenderingBenchmarks*
    /// </summary>
    [MemoryDiagnoser]
    public class iOSRenderingBenchmarks
    {
        private const int FrameCount = 1000;

        // iOS TextInputContextIdentifier: new Guid+string+wrapper vs cached (TextInputResponder.cs)

        [Benchmark]
        public string? Current_TextInputContextId_NewGuidPerAccess()
        {
            string? last = null;
            for (int i = 0; i < FrameCount; i++)
            {
                last = Guid.NewGuid().ToString();
            }
            return last;
        }

        [Benchmark]
        public string? Optimized_TextInputContextId_Cached()
        {
            string cached = Guid.NewGuid().ToString();
            string? last = null;
            for (int i = 0; i < FrameCount; i++)
            {
                last = cached;
            }
            return last;
        }

    }
}
