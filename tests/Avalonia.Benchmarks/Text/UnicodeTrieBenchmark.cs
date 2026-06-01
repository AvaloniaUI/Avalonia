using System;
using Avalonia.Media.TextFormatting.Unicode;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Text;

/// <summary>
/// Raw <see cref="UnicodeTrie.Get"/> throughput across the four committed tries
/// (UnicodeData, BiDi, GraphemeBreak, EastAsianWidth) and three codepoint
/// distributions (ASCII, BMP, supplementary). Bypasses surrogate decoding so
/// the numbers reflect only the trie walk cost — useful as a regression target
/// when changing trie packing, branch ordering, or builder layout.
/// </summary>
[MemoryDiagnoser]
[MinIterationTime(150)]
[MaxWarmupCount(15)]
public class UnicodeTrieBenchmark
{
    public enum CodepointDistribution
    {
        /// <summary>Printable ASCII (0x20..0x7E) — common BMP-non-surrogate path.</summary>
        Ascii,

        /// <summary>Full BMP excluding the surrogate range (0..0xD7FF, 0xE000..0xFFFF).</summary>
        Bmp,

        /// <summary>Supplementary plane (0x10000..0x10FFFF) — exercises two-level lookup.</summary>
        Supplementary,
    }

    private const int CodepointCount = 4096;

    private uint[] _codepoints = [];

    [Params(CodepointDistribution.Ascii, CodepointDistribution.Bmp, CodepointDistribution.Supplementary)]
    public CodepointDistribution Distribution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Fixed seed for reproducibility — numbers across CI runs should be
        // comparable in the absence of generator / trie-walk changes.
        var rng = new Random(42);
        _codepoints = new uint[CodepointCount];

        for (var i = 0; i < CodepointCount; i++)
        {
            _codepoints[i] = Distribution switch
            {
                CodepointDistribution.Ascii => (uint)rng.Next(0x20, 0x7F),
                CodepointDistribution.Bmp => SampleBmp(rng),
                CodepointDistribution.Supplementary => (uint)rng.Next(0x10000, 0x110000),
                _ => 0,
            };
        }
    }

    private static uint SampleBmp(Random rng)
    {
        // Avoid the surrogate range — supplementary callers don't hit it in
        // practice and we cover it via the dedicated codepoint fixture below.
        var v = (uint)rng.Next(0, 0xFFFE);
        return v is >= 0xD800 and <= 0xDFFF ? 0xE000 : v;
    }

    [Benchmark]
    public uint Get_UnicodeData()
    {
        var trie = UnicodeDataTrie.Trie;
        var codepoints = _codepoints;
        var sum = 0u;

        for (var i = 0; i < codepoints.Length; i++)
        {
            sum += trie.Get(codepoints[i]);
        }

        return sum;
    }

    [Benchmark]
    public uint Get_BiDi()
    {
        var trie = BiDiTrie.Trie;
        var codepoints = _codepoints;
        var sum = 0u;

        for (var i = 0; i < codepoints.Length; i++)
        {
            sum += trie.Get(codepoints[i]);
        }

        return sum;
    }

    [Benchmark]
    public uint Get_GraphemeBreak()
    {
        var trie = GraphemeBreakTrie.Trie;
        var codepoints = _codepoints;
        var sum = 0u;

        for (var i = 0; i < codepoints.Length; i++)
        {
            sum += trie.Get(codepoints[i]);
        }

        return sum;
    }

    [Benchmark]
    public uint Get_EastAsianWidth()
    {
        var trie = EastAsianWidthTrie.Trie;
        var codepoints = _codepoints;
        var sum = 0u;

        for (var i = 0; i < codepoints.Length; i++)
        {
            sum += trie.Get(codepoints[i]);
        }

        return sum;
    }

    /// <summary>
    /// Worst-case "every property" pass — one <see cref="UnicodeTrie.Get"/> call
    /// per trie per codepoint. Matches what a layout pipeline pass that needs
    /// every property would pay if the four tries are kept separate. Establishes
    /// a baseline for any future "merge packed properties" experiment.
    /// </summary>
    [Benchmark]
    public uint Get_AllTriesPerCodepoint()
    {
        var unicodeData = UnicodeDataTrie.Trie;
        var biDi = BiDiTrie.Trie;
        var grapheme = GraphemeBreakTrie.Trie;
        var eaw = EastAsianWidthTrie.Trie;
        var codepoints = _codepoints;
        var sum = 0u;

        for (var i = 0; i < codepoints.Length; i++)
        {
            var cp = codepoints[i];
            sum += unicodeData.Get(cp);
            sum += biDi.Get(cp);
            sum += grapheme.Get(cp);
            sum += eaw.Get(cp);
        }

        return sum;
    }
}
