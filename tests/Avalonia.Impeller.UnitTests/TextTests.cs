using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Xunit;
using static Avalonia.Impeller.UnitTests.ImpellerTestApp;

namespace Avalonia.Impeller.UnitTests;

public class TextTests
{
    [Fact]
    public void Text_Is_Drawn_Inside_Its_Bounds()
    {
        using var app = Start();
        var typeface = Typeface.Default.GlyphTypeface;
        var shaped = TextShaper.Current.ShapeText("Hi",
            new TextShaperOptions(typeface, 40, 0, CultureInfo.InvariantCulture));
        var run = new GlyphRun(typeface, 40, "Hi".AsMemory(), shaped, new Point(5, 45));
        var bounds = run.InkBounds;

        using var bitmap = new RenderTargetBitmap(new PixelSize(80, 60));
        using (var context = bitmap.CreateDrawingContext())
            context.DrawGlyphRun(Brushes.Black, run);

        var pixels = ReadPixels(bitmap);
        var inside = 0;
        var outside = 0;
        for (var y = 0; y < 60; y++)
            for (var x = 0; x < 80; x++)
                if (Pixel(pixels, 80, x, y).A > 0)
                {
                    if (bounds.Inflate(1).Contains(new Point(x + 0.5, y + 0.5)))
                        inside++;
                    else
                        outside++;
                }

        Assert.True(inside > 100, $"Only {inside} pixels of ink.");
        Assert.Equal(0, outside);
    }
}
