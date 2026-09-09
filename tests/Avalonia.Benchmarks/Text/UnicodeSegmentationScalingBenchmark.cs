using System;
using System.Text;
using Avalonia.Media.TextFormatting.Unicode;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Text;

/// <summary>
/// Word and sentence segmentation over text shapes whose per-character cost differs: ordinary
/// prose, a script without spaces, emoji sequences carrying joiners, and the long runs of spaces
/// or closing punctuation that the sentence rules have to look through. Each shape is measured at
/// two lengths, so the cost per character is comparable across them: an enumerator that stays
/// linear holds that figure between the two lengths, one that rescans does not.
/// </summary>
[MemoryDiagnoser]
[MinIterationTime(150)]
[MaxWarmupCount(15)]
public class UnicodeSegmentationScalingBenchmark
{
    public enum TextShape
    {
        /// <summary>English prose with abbreviations, quotes and decimals.</summary>
        Prose,

        /// <summary>Han text with ideographic full stops and no spaces.</summary>
        Cjk,

        /// <summary>Emoji sequences joined by ZWJ, interleaved with ASCII.</summary>
        Emoji,

        /// <summary>Nothing but spaces.</summary>
        SpaceRun,

        /// <summary>A letter followed by closing punctuation.</summary>
        CloseRun,

        /// <summary>A sentence terminator followed by spaces, holding the terminator context open.</summary>
        TerminatorRun,

        /// <summary>Flags, so every codepoint is a regional indicator.</summary>
        FlagRun,
    }

    private const string ProseSample =
        "Dr. Smith went home. He slept until 6.30 a.m. and read \"The U.S.A. Today\" for an hour! Was it worth it? Probably not. ";

    private const string CjkSample =
        "这是一个测试。我们需要更多的文本来测量分段。";

    // Woman technologist, then a four-person family, both built from ZWJ (Format) sequences.
    private const string EmojiSample =
        "\U0001F469\u200D\U0001F4BB \U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466 \U0001F389 Ship it. ";

    private const string FlagSample =
        "\U0001F1E9\U0001F1EA\U0001F1EB\U0001F1F7\U0001F1EE\U0001F1F9\U0001F1EA\U0001F1F8";

    private string _text = string.Empty;

    [Params(
        TextShape.Prose,
        TextShape.Cjk,
        TextShape.Emoji,
        TextShape.SpaceRun,
        TextShape.CloseRun,
        TextShape.TerminatorRun,
        TextShape.FlagRun)]
    public TextShape Shape { get; set; }

    [Params(1024, 16384)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _text = Shape switch
        {
            TextShape.Prose => Tile(ProseSample, Length),
            TextShape.Cjk => Tile(CjkSample, Length),
            TextShape.Emoji => Tile(EmojiSample, Length),
            TextShape.SpaceRun => new string(' ', Length),
            TextShape.CloseRun => "A" + new string(')', Length - 1),
            TextShape.TerminatorRun => "A." + new string(' ', Length - 2),
            TextShape.FlagRun => Tile(FlagSample, Length),
            _ => string.Empty,
        };
    }

    [Benchmark]
    public int WordBreak()
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
    public int SentenceBreak()
    {
        var enumerator = new SentenceBreakEnumerator(_text.AsSpan());
        var count = 0;

        while (enumerator.MoveNext(out _))
        {
            count++;
        }

        return count;
    }

    // Repeats the sample to the requested code-unit length, cutting back to a codepoint
    // boundary so a supplementary-plane scalar is never split into a lone surrogate.
    private static string Tile(string sample, int length)
    {
        var builder = new StringBuilder(length + sample.Length);

        while (builder.Length < length)
        {
            builder.Append(sample);
        }

        var end = length;

        if (char.IsHighSurrogate(builder[end - 1]))
        {
            end--;
        }

        return builder.ToString(0, end);
    }
}
