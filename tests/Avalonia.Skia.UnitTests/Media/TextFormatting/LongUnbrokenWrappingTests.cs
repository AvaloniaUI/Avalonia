using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting;

public sealed class LongUnbrokenWrappingTests
{
    [Theory]
    [InlineData("unbrokenwordwithseveralletters", false)]
    [InlineData("unbrokenwordwithseveralletters", true)]
    [InlineData("one  two   three   ", false)]
    [InlineData("one  two   three   ", true)]
    [InlineData("'quoted text' (123,456.78) next", false)]
    [InlineData("'quoted text' (123,456.78) next", true)]
    [InlineData("日本語の文章と句読点。続き", false)]
    [InlineData("日本語の文章と句読点。続き", true)]
    [InlineData("مرحبا بالعالم العربية", false)]
    [InlineData("مرحبا بالعالم العربية", true)]
    [InlineData("বাংলা লেখা এবং শব্দ", false)]
    [InlineData("বাংলা লেখা এবং শব্দ", true)]
    [InlineData("emoji 😀 👩‍💻 🇨🇦🇯🇵 combining e\u0301", false)]
    [InlineData("emoji 😀 👩‍💻 🇨🇦🇯🇵 combining e\u0301", true)]
    [InlineData("word\u00a0word\u2060word\u200bword\u00adword", false)]
    [InlineData("word\u00a0word\u2060word\u200bword\u00adword", true)]
    [InlineData("a (12.34) 56,789% +99 /next", false)]
    [InlineData("a (12.34) 56,789% +99 /next", true)]
    public void Split_Buffer_Opportunities_Match_Unicode_Boundaries(string text, bool warmCache)
    {
        using var application = TextFormatterTests.Start();
        var properties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
        var source = new SingleBufferTextSource(text, properties);
        using var line = new TextFormatterImpl().FormatLine(source, 0, double.PositiveInfinity,
            new GenericTextParagraphProperties(properties, textWrapping: TextWrapping.NoWrap));
        Assert.NotNull(line);
        foreach (var run in line.TextRuns)
        {
            if (run is not ShapedTextRun shaped) continue;
            var original = shaped.ShapedBuffer;
            if (warmCache) Check(original, "original");
            using var alias = original.WithBidiLevel((sbyte)(original.BidiLevel ^ 1));
            using var writable = original.CloneWritable();
            Check(alias, "Bidi alias");
            Check(writable, "writable clone");
            for (int split = 1; split < original.Text.Length; split++)
            {
                var pieces = original.Split(split);
                foreach (var child in new[] { pieces.First, pieces.Second })
                {
                    if (child is null) continue;
                    try { Check(child, "split " + split); }
                    finally { if (!ReferenceEquals(child, original)) child.Dispose(); }
                }
            }
        }
    }

    private static void Check(ShapedBuffer buffer, string context)
    {
        var expected = Fresh(buffer.Text.Span);
        var actual = Cached(buffer);
        Assert.True(expected.SequenceEqual(actual), context + ": " + buffer.Text.ToString() +
            " expected=" + string.Join(";", expected) + " actual=" + string.Join(";", actual));
    }

    [Fact]
    public void Split_Children_Retain_Their_Unicode_Opportunities_After_The_Owner_Is_Disposed()
    {
        using var application = TextFormatterTests.Start();
        var properties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
        var source = new SingleBufferTextSource("abcdefghijklmnopqrstuv", properties);
        var line = new TextFormatterImpl().FormatLine(source, 0, double.PositiveInfinity,
            new GenericTextParagraphProperties(properties, textWrapping: TextWrapping.NoWrap));
        try
        {
            Assert.NotNull(line);
            var original = Assert.IsType<ShapedTextRun>(Assert.Single(line.TextRuns)).ShapedBuffer;
            _ = Cached(original);
            var split = original.Split(7);
            using var first = split.First;
            using var second = split.Second;
            line.Dispose(); line = null;
            Assert.NotNull(first); Assert.NotNull(second);
            Check(first, "leading child after owner disposal");
            Check(second, "trailing child after owner disposal");
        }
        finally { line?.Dispose(); }
    }

    private static LineBreak[] Fresh(ReadOnlySpan<char> text)
    {
        var result = new List<LineBreak>();
        var iterator = new LineBreakEnumerator(text);
        while (iterator.MoveNext(out var value)) result.Add(value);
        return result.ToArray();
    }

    private static LineBreak[] Cached(ShapedBuffer buffer)
    {
        var result = new List<LineBreak>();
        var iterator = buffer.GetCachedLineBreakEnumerator();
        while (iterator.MoveNext(out var value)) result.Add(value);
        return result.ToArray();
    }

    [Fact]
    public void A_Near_Limit_Unbroken_Paragraph_Wraps_Without_Quadratic_Rescanning()
    {
        using var application = TextFormatterTests.Start();
        var text = new string('x', 900_000);
        var properties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
        var paragraph = new GenericTextParagraphProperties(properties, textWrapping: TextWrapping.Wrap);
        var source = new SingleBufferTextSource(text, properties);
        var formatter = new TextFormatterImpl();
        var timer = Stopwatch.StartNew();
        TextLineBreak? previous = null;
        int position = 0, lines = 0;
        while (position < text.Length)
        {
            using var line = formatter.FormatLine(source, position, 800, paragraph, previous);
            Assert.NotNull(line);
            Assert.True(line.Length > 0);
            Assert.True(line.Width <= 801);
            position += line.Length;
            previous = line.TextLineBreak;
            lines++;
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(8),
                $"Wrapping stalled after {position} of {text.Length} characters across {lines} lines.");
        }
        Assert.Equal(text.Length, position);
        Assert.True(lines > 1000);
    }
}
