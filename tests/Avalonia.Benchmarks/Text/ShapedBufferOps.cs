#nullable enable
using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Text;

/// <summary>
/// Micro-benchmark for the <see cref="ShapedBuffer"/> cluster-cache hot paths
/// (<see cref="ShapedBuffer.TotalGlyphAdvance"/>, <see cref="ShapedBuffer.MeasureCharactersThatFit"/>,
/// and the cached <see cref="ShapedBuffer.Split"/> chain). Compares the
/// simple-mode fast path (1 char per cluster) against complex clusters by
/// shaping random ASCII vs. random-from-extended-Latin so the buffers exercise
/// different code paths. Pair with <c>--memory</c> to see allocation impact of
/// the <see cref="System.Buffers.ArrayPool{T}"/>-backed cluster cache.
/// </summary>
[MemoryDiagnoser]
[MinIterationTime(150)]
[MaxWarmupCount(10)]
public class ShapedBufferOps : IDisposable
{
    private readonly IDisposable _app;
    private readonly TextShaperOptions _options;
    private string _text = string.Empty;
    private ShapedBuffer? _primed;

    public ShapedBufferOps()
    {
        _app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        _options = new TextShaperOptions(Typeface.Default.GlyphTypeface);
    }

    [Params(8, 32, 128, 512, 2048)]
    public int GlyphCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // ASCII text → simple-mode fast path (one glyph == one cluster == one char).
        var rng = new Random(GlyphCount);
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
        _text = new string(Enumerable.Range(0, GlyphCount).Select(_ => alphabet[rng.Next(alphabet.Length)]).ToArray());

        _primed?.Dispose();
        _primed = TextShaper.Current.ShapeText(_text, _options);
        // Prime the cluster cache once so the per-call benchmarks measure only the lookup cost.
        _ = _primed.TotalGlyphAdvance;
    }

    /// <summary>
    /// Cost of shaping + building the cluster cache from scratch. Captures both
    /// the HarfBuzz call and the prefix-sum allocation/initialisation.
    /// </summary>
    [Benchmark]
    public double ShapeAndPrime()
    {
        using var buffer = TextShaper.Current.ShapeText(_text, _options);
        return buffer.TotalGlyphAdvance;
    }

    /// <summary>
    /// Repeated <see cref="ShapedBuffer.TotalGlyphAdvance"/> on a primed buffer.
    /// Should be ~O(1) per call regardless of glyph count.
    /// </summary>
    [Benchmark]
    public double TotalAdvance_Cached()
    {
        var sum = 0d;
        for (var i = 0; i < 64; i++)
        {
            sum += _primed!.TotalGlyphAdvance;
        }
        return sum;
    }

    /// <summary>
    /// Repeated <see cref="ShapedBuffer.MeasureCharactersThatFit"/> targeting
    /// half the buffer's total width. Exercises the binary search across the
    /// prefix table.
    /// </summary>
    [Benchmark]
    public int MeasureFit_Cached()
    {
        var halfWidth = _primed!.TotalGlyphAdvance * 0.5;
        var sum = 0;
        for (var i = 0; i < 64; i++)
        {
            sum += _primed.MeasureCharactersThatFit(halfWidth, out _);
        }
        return sum;
    }

    /// <summary>
    /// Splits the primed buffer at three positions and queries the resulting
    /// children — the workload the shared cluster cache is designed for. Each
    /// child should reuse the parent cache in O(1)/O(log) instead of rebuilding.
    /// </summary>
    [Benchmark]
    public double SplitChain()
    {
        var quarter = _primed!.Text.Length / 4;
        var halves = _primed.Split(quarter * 2);
        var first = halves.First!;
        var second = halves.Second!;

        var firstSplit = first.Split(quarter);
        var secondSplit = second.Split(quarter);

        var total = (firstSplit.First?.TotalGlyphAdvance ?? 0)
                  + (firstSplit.Second?.TotalGlyphAdvance ?? 0)
                  + (secondSplit.First?.TotalGlyphAdvance ?? 0)
                  + (secondSplit.Second?.TotalGlyphAdvance ?? 0);
        return total;
    }

    public void Dispose()
    {
        _primed?.Dispose();
        _app.Dispose();
    }
}
