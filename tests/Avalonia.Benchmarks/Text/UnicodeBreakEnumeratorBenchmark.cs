using System;
using System.Text;
using Avalonia.Media.TextFormatting.Unicode;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Text;

/// <summary>
/// End-to-end iteration cost for the three Unicode break enumerators. Their
/// inner loops call into the trie-backed property getters, so this benchmark
/// captures both the trie lookup cost and the per-segment algorithmic overhead
/// in a shape that mirrors what text layout pays per string.
/// </summary>
[MemoryDiagnoser]
[MinIterationTime(150)]
[MaxWarmupCount(15)]
public class UnicodeBreakEnumeratorBenchmark
{
    public enum TextDistribution
    {
        /// <summary>Pure ASCII — no surrogate decoding cost.</summary>
        Ascii,

        /// <summary>BMP mix of Latin, Cyrillic, Greek, CJK.</summary>
        Bmp,

        /// <summary>Mix of BMP and supplementary plane scalars.</summary>
        Supplementary,
    }

    private const int ScalarCount = 1024;

    private string _text = string.Empty;

    [Params(TextDistribution.Ascii, TextDistribution.Bmp, TextDistribution.Supplementary)]
    public TextDistribution Distribution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // The fixture intentionally mirrors CodepointBenchmark's distributions so
        // the two benchmark suites are comparable: any added cost above the raw
        // codepoint sequence is the break-algorithm overhead.
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
    public int LineBreakEnumerator_Sequence()
    {
        var enumerator = new LineBreakEnumerator(_text.AsSpan());
        var count = 0;

        while (enumerator.MoveNext(out _))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int WordBreakEnumerator_Sequence()
    {
        var enumerator = new WordBreakEnumerator(_text.AsSpan());
        var count = 0;

        while (enumerator.MoveNext(out _))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int GraphemeEnumerator_Sequence()
    {
        var enumerator = new GraphemeEnumerator(_text.AsSpan());
        var count = 0;

        while (enumerator.MoveNext(out _))
        {
            count++;
        }

        return count;
    }
}
