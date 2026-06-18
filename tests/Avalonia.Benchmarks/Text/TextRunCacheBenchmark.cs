using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Text;

[MemoryDiagnoser]
[MinIterationTime(150)]
[MaxWarmupCount(15)]
public class TextRunCacheBenchmark : IDisposable
{
    private readonly IDisposable _app;

    private const string ShortText = "The quick brown fox jumps over the lazy dog.";

    private const string LongText =
        "Though, the objectives of the development of the prominent landmarks can be neglected in most cases, " +
        "it should be realized that after the completion of the strategic decision gives rise to " +
        "The Expertise of Regular Program. A number of key issues arise from the belief that the explicit " +
        "examination of strategic management should correlate with the conceptual design. " +
        "By all means, the unification of the reliably developed techniques indicates the importance of " +
        "the ultimate advantage of episodic skill over alternate practices.";

    public TextRunCacheBenchmark()
    {
        _app = UnitTestApplication.Start(TestServices.StyledWindow);
    }

    [Params(5, 20)]
    public int Iterations { get; set; }

    [Benchmark(Baseline = true)]
    public void LayoutWithoutCache_Short()
    {
        for (var i = 0; i < Iterations; i++)
        {
            using var layout = new TextLayout(ShortText, Typeface.Default, 12d, Brushes.Black,
                maxWidth: 200, textWrapping: TextWrapping.WrapWithOverflow);
        }
    }

    [Benchmark]
    public void LayoutWithCache_Short()
    {
        using var cache = new TextRunCache();

        for (var i = 0; i < Iterations; i++)
        {
            using var layout = new TextLayout(ShortText, Typeface.Default, 12d, Brushes.Black,
                maxWidth: 200, textWrapping: TextWrapping.WrapWithOverflow, textRunCache: cache);
        }
    }

    [Benchmark]
    public void LayoutWithoutCache_Long()
    {
        for (var i = 0; i < Iterations; i++)
        {
            using var layout = new TextLayout(LongText, Typeface.Default, 12d, Brushes.Black,
                maxWidth: 300, textWrapping: TextWrapping.WrapWithOverflow);
        }
    }

    [Benchmark]
    public void LayoutWithCache_Long()
    {
        using var cache = new TextRunCache();

        for (var i = 0; i < Iterations; i++)
        {
            using var layout = new TextLayout(LongText, Typeface.Default, 12d, Brushes.Black,
                maxWidth: 300, textWrapping: TextWrapping.WrapWithOverflow, textRunCache: cache);
        }
    }

    [Benchmark]
    public void LayoutWithoutCache_VaryingWidth()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var width = 200 + i * 10;

            using var layout = new TextLayout(LongText, Typeface.Default, 12d, Brushes.Black,
                maxWidth: width, textWrapping: TextWrapping.WrapWithOverflow);
        }
    }

    [Benchmark]
    public void LayoutWithCache_VaryingWidth()
    {
        using var cache = new TextRunCache();

        for (var i = 0; i < Iterations; i++)
        {
            var width = 200 + i * 10;

            using var layout = new TextLayout(LongText, Typeface.Default, 12d, Brushes.Black,
                maxWidth: width, textWrapping: TextWrapping.WrapWithOverflow, textRunCache: cache);
        }
    }

    /// <summary>
    /// Benchmarks the single-entry fast path: a simple single-paragraph text
    /// that results in only one cache entry (the common case for TextBlock).
    /// </summary>
    [Benchmark]
    public void LayoutWithCache_SingleEntry_Short()
    {
        using var cache = new TextRunCache();

        for (var i = 0; i < Iterations; i++)
        {
            // NoWrap + single paragraph = single cache entry at index 0.
            using var layout = new TextLayout(ShortText, Typeface.Default, 12d, Brushes.Black,
                maxWidth: double.PositiveInfinity, textWrapping: TextWrapping.NoWrap, textRunCache: cache);
        }
    }

    /// <summary>
    /// Benchmarks the single-entry fast path with invalidate/re-populate cycle,
    /// verifying that the inline store is reused without dictionary allocation.
    /// </summary>
    [Benchmark]
    public void LayoutWithCache_SingleEntry_InvalidateRepopulate()
    {
        using var cache = new TextRunCache();

        for (var i = 0; i < Iterations; i++)
        {
            cache.Invalidate();

            using var layout = new TextLayout(ShortText, Typeface.Default, 12d, Brushes.Black,
                maxWidth: double.PositiveInfinity, textWrapping: TextWrapping.NoWrap, textRunCache: cache);
        }
    }

    public void Dispose()
    {
        _app?.Dispose();
    }
}
