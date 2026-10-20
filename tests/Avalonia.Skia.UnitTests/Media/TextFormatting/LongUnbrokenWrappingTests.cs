using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting;

public sealed class LongUnbrokenWrappingTests
{
    [Theory]
    [InlineData('x')]
    [InlineData('\u0628')]
    public void A_Near_Limit_Unbroken_Paragraph_Wraps_Without_Quadratic_Rescanning(char character)
    {
        using var application = TextFormatterTests.Start();
        var text = new string(character, 900_000);
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

    [Theory]
    [InlineData("short ", false)]
    [InlineData("short ", true)]
    [InlineData("short     ", false)]
    [InlineData("short     ", true)]
    public void Strict_Wrapping_Retains_The_Last_Fitting_Break_Before_A_Long_Word(string prefix, bool styled)
    {
        using var application = TextFormatterTests.Start();
        var properties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
        var tailProperties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Red);
        var tail = new string('x', 2_000);
        ITextSource source = styled
            ? new TextFormatterTests.ListTextSource(new TextCharacters(prefix, properties),
                new TextCharacters(tail, tailProperties))
            : new SingleBufferTextSource(prefix + tail, properties);
        var formatter = new TextFormatterImpl();
        var paragraph = new GenericTextParagraphProperties(properties, textWrapping: TextWrapping.Wrap);
        using var first = formatter.FormatLine(source, 0, 100, paragraph);
        Assert.NotNull(first);
        Assert.Equal(prefix.Length, first.Length);
        using var second = formatter.FormatLine(source, first.Length, 100, paragraph, first.TextLineBreak);
        Assert.NotNull(second);
        Assert.InRange(second.Length, 1, tail.Length - 1);
    }

    [Theory]
    [InlineData("e\u0301", 2)]
    [InlineData("👩‍💻", 5)]
    [InlineData("🇨🇦", 4)]
    public void A_Zero_Width_Line_Still_Consumes_The_Complete_First_Grapheme(string grapheme, int length)
    {
        using var application = TextFormatterTests.Start();
        var properties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
        var source = new SingleBufferTextSource(grapheme + new string('x', 2_000), properties);
        using var line = new TextFormatterImpl().FormatLine(source, 0, 0,
            new GenericTextParagraphProperties(properties, textWrapping: TextWrapping.Wrap));
        Assert.NotNull(line);
        Assert.Equal(length, line.Length);
    }
}
