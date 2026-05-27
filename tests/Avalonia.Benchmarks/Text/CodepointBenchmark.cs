using System;
using System.Text;
using Avalonia.Media.TextFormatting.Unicode;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Text;

/// <summary>
/// End-to-end benchmarks for the <see cref="Codepoint"/> hot path: surrogate
/// decoding via <see cref="Codepoint.ReadAt"/>, property accessors that
/// perform Unicode data lookups via generated static <see cref="UnicodeTrie"/>
/// instances, and a representative "all properties" pass that approximates
/// what a layout pipeline traversal pays per codepoint.
/// </summary>
[MemoryDiagnoser]
[MinIterationTime(150)]
[MaxWarmupCount(15)]
public class CodepointBenchmark
{
    public enum TextDistribution
    {
        /// <summary>Pure ASCII (no surrogate decoding cost).</summary>
        Ascii,

        /// <summary>BMP mix of Latin, Cyrillic, Greek, CJK (one UTF-16 unit per scalar).</summary>
        Bmp,

        /// <summary>Mix of supplementary plane scalars (math, emoji, CJK Ext B) — every other
        /// scalar requires surrogate-pair decoding.</summary>
        Supplementary,
    }

    private const int ScalarCount = 1024;

    private string _text = string.Empty;

    [Params(TextDistribution.Ascii, TextDistribution.Bmp, TextDistribution.Supplementary)]
    public TextDistribution Distribution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        var sb = new StringBuilder(ScalarCount * 2);

        for (var i = 0; i < ScalarCount; i++)
        {
            var scalar = Distribution switch
            {
                TextDistribution.Ascii => (uint)rng.Next(0x20, 0x7F),
                TextDistribution.Bmp => SampleBmpScalar(rng),
                TextDistribution.Supplementary => SampleSupplementaryScalar(rng),
                _ => (uint)'a',
            };

            sb.Append(char.ConvertFromUtf32((int)scalar));
        }

        _text = sb.ToString();
    }

    private static uint SampleBmpScalar(Random rng)
    {
        // Pick from a spread of common BMP ranges; skip the surrogate region.
        var bucket = rng.Next(4);
        return bucket switch
        {
            0 => (uint)rng.Next(0x0020, 0x007F),  // Latin
            1 => (uint)rng.Next(0x0400, 0x0500),  // Cyrillic
            2 => (uint)rng.Next(0x0370, 0x0400),  // Greek
            _ => (uint)rng.Next(0x4E00, 0x9FFF),  // CJK Unified Ideographs
        };
    }

    private static uint SampleSupplementaryScalar(Random rng)
    {
        // Alternate BMP and supplementary so the benchmark exercises both
        // branches of Codepoint.ReadAt — pure-supplementary text isn't
        // representative of any real layout workload.
        if ((rng.Next() & 1) == 0)
        {
            return SampleBmpScalar(rng);
        }

        var bucket = rng.Next(3);
        return bucket switch
        {
            0 => (uint)rng.Next(0x1F300, 0x1F600),  // emoji
            1 => (uint)rng.Next(0x20000, 0x2A6DF),  // CJK Ext B
            _ => (uint)rng.Next(0x1D400, 0x1D800),  // math alphanumerics
        };
    }

    [Benchmark]
    public uint ReadAt_Sequence()
    {
        var span = _text.AsSpan();
        var sum = 0u;

        var i = 0;
        while (i < span.Length)
        {
            sum += Codepoint.ReadAt(span, i, out var count).Value;
            i += count;
        }

        return sum;
    }

    [Benchmark]
    public uint CodepointEnumerator_Sequence()
    {
        var enumerator = new CodepointEnumerator(_text.AsSpan());
        var sum = 0u;

        while (enumerator.MoveNext(out var cp))
        {
            sum += cp.Value;
        }

        return sum;
    }

    [Benchmark]
    public int Sequence_GeneralCategory()
    {
        var span = _text.AsSpan();
        var sum = 0;

        var i = 0;
        while (i < span.Length)
        {
            var cp = Codepoint.ReadAt(span, i, out var count);
            sum += (int)cp.GeneralCategory;
            i += count;
        }

        return sum;
    }

    [Benchmark]
    public int Sequence_Script()
    {
        var span = _text.AsSpan();
        var sum = 0;

        var i = 0;
        while (i < span.Length)
        {
            var cp = Codepoint.ReadAt(span, i, out var count);
            sum += (int)cp.Script;
            i += count;
        }

        return sum;
    }

    [Benchmark]
    public int Sequence_BiDiClass()
    {
        var span = _text.AsSpan();
        var sum = 0;

        var i = 0;
        while (i < span.Length)
        {
            var cp = Codepoint.ReadAt(span, i, out var count);
            sum += (int)cp.BiDiClass;
            i += count;
        }

        return sum;
    }

    /// <summary>
    /// Worst-case representative of a layout pipeline pass that needs every
    /// property per codepoint. Seven trie lookups per scalar (Category +
    /// Script + BiDi + LineBreak + WordBreak + GraphemeBreak + EastAsianWidth).
    /// This is the headline number for the "should we merge tries?" question.
    /// </summary>
    [Benchmark]
    public int Sequence_AllProperties()
    {
        var span = _text.AsSpan();
        var sum = 0;

        var i = 0;
        while (i < span.Length)
        {
            var cp = Codepoint.ReadAt(span, i, out var count);
            sum += (int)cp.GeneralCategory;
            sum += (int)cp.Script;
            sum += (int)cp.BiDiClass;
            sum += (int)cp.LineBreakClass;
            sum += (int)cp.WordBreakClass;
            sum += (int)cp.GraphemeBreakClass;
            sum += (int)cp.EastAsianWidthClass;
            i += count;
        }

        return sum;
    }

    [Benchmark]
    public int TryGetPairedBracket_Sequence()
    {
        var span = _text.AsSpan();
        var paired = 0;

        var i = 0;
        while (i < span.Length)
        {
            var cp = Codepoint.ReadAt(span, i, out var count);
            if (cp.TryGetPairedBracket(out _))
            {
                paired++;
            }
            i += count;
        }

        return paired;
    }
}
