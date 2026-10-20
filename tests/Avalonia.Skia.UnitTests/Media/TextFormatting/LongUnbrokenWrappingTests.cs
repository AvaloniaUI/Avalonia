using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting;

public sealed class LongUnbrokenWrappingTests
{
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
