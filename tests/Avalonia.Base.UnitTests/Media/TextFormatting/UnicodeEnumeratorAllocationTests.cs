using System;
using System.Linq;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

/// <summary>
/// Locks the "no managed allocations on the hot enumeration path" contract for
/// the three Unicode break enumerators. Uses
/// <see cref="GC.GetAllocatedBytesForCurrentThread"/> as the ground truth,
/// which is more reliable than BenchmarkDotNet's <c>MemoryDiagnoser</c> when
/// the latter runs in-process (its own bookkeeping leaks into the per-op
/// allocation count). Any future change that introduces a per-iteration
/// allocation in <see cref="LineBreakEnumerator"/>, <see cref="WordBreakEnumerator"/>,
/// or <see cref="GraphemeEnumerator"/> fails this test instead of hiding behind
/// benchmark noise.
/// </summary>
public class UnicodeEnumeratorAllocationTests
{
    private const int WarmupIterations = 100;
    private const int MeasureIterations = 1000;

    [Fact]
    public void LineBreakEnumerator_DoesNotAllocate()
    {
        var text = BuildSampleText();

        // Warm up: JIT every hot path, fault in any lazily-initialised statics.
        for (var i = 0; i < WarmupIterations; i++)
        {
            var w = new LineBreakEnumerator(text.AsSpan());
            while (w.MoveNext(out _)) { }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < MeasureIterations; i++)
        {
            var e = new LineBreakEnumerator(text.AsSpan());
            while (e.MoveNext(out _)) { }
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void WordBreakEnumerator_DoesNotAllocate()
    {
        var text = BuildSampleText();

        for (var i = 0; i < WarmupIterations; i++)
        {
            var w = new WordBreakEnumerator(text.AsSpan());
            while (w.MoveNext(out _)) { }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < MeasureIterations; i++)
        {
            var e = new WordBreakEnumerator(text.AsSpan());
            while (e.MoveNext(out _)) { }
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void GraphemeEnumerator_DoesNotAllocate()
    {
        var text = BuildSampleText();

        for (var i = 0; i < WarmupIterations; i++)
        {
            var w = new GraphemeEnumerator(text.AsSpan());
            while (w.MoveNext(out _)) { }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < MeasureIterations; i++)
        {
            var e = new GraphemeEnumerator(text.AsSpan());
            while (e.MoveNext(out _)) { }
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// Mixed Latin / Cyrillic / Greek / CJK in the BMP — same distribution as
    /// the benchmark's <c>Bmp</c> fixture. Exercising the non-ASCII paths in
    /// the enumerators is the case where any latent allocation is most likely
    /// to surface.
    /// </summary>
    private static string BuildSampleText()
    {
        var rng = new Random(42);
        return new string(Enumerable.Range(0, 1024).Select(_ =>
        {
            var bucket = rng.Next(4);
            return bucket switch
            {
                0 => (char)rng.Next(0x0020, 0x007F),
                1 => (char)rng.Next(0x0400, 0x0500),
                2 => (char)rng.Next(0x0370, 0x0400),
                _ => (char)rng.Next(0x4E00, 0x9FFF),
            };
        }).ToArray());
    }
}
