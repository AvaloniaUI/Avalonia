using System;
using System.Threading.Tasks;
using Avalonia.Collections.Pooled;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.ColorPickerTests;

/// <summary>
/// Tests that <see cref="ColorPickerHelpers.CreateComponentBitmapAsync"/> produces
/// accurate pixel output when <c>isPerceptive=false</c> — the mode the ColorSpectrum's
/// third-component slider should use.
///
/// Regression tests for https://github.com/AvaloniaUI/Avalonia/issues/20925
/// </summary>
public class ColorPickerHelpersTests : ScopedTestBase
{
    /// <summary>
    /// When sweeping Hue at a fixed desaturated selection (S=0.2, V=0.5),
    /// every pixel's brightest channel should be bounded by V — not boosted to 255.
    /// </summary>
    [Fact]
    public async Task Hue_Slider_Bitmap_Reflects_Actual_Saturation_And_Value()
    {
        var selection = new HsvColor(alpha: 1.0, hue: 180, saturation: 0.2, value: 0.5);

        using var pixels = await GenerateSliderBitmap(ColorComponent.Component1, selection, height: 360);

        // Sample mid-strip. In HSV, max RGB channel = V for any hue.
        var (r, g, b) = ReadPixel(pixels, row: 180);
        var maxChannel = Math.Max(r, Math.Max(g, b));
        double expectedMax = selection.V * 255;

        Assert.InRange(maxChannel, expectedMax - 10, expectedMax + 10);
    }

    /// <summary>
    /// When sweeping Saturation, the brightest channel at full saturation is bounded
    /// by the current Value — not forced to 255 by perceptive V=1.0 override.
    /// </summary>
    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(0.9)]
    public async Task Saturation_Slider_Bitmap_Respects_Current_Value(double currentValue)
    {
        var selection = new HsvColor(alpha: 1.0, hue: 200, saturation: 0.8, value: currentValue);

        using var pixels = await GenerateSliderBitmap(ColorComponent.Component2, selection);

        // Row 0 = highest saturation (bitmap sweeps high→low top-to-bottom).
        var (r, g, b) = ReadPixel(pixels, row: 0);
        var maxChannel = Math.Max(r, Math.Max(g, b));
        double expectedMax = currentValue * 255;

        Assert.InRange(maxChannel, expectedMax - 20, expectedMax + 20);
    }

    /// <summary>
    /// When sweeping Value at low Saturation (S=0.1), the top pixel should be
    /// near-grey (low chroma) — not vivid from a perceptive S=1.0 override.
    /// </summary>
    [Fact]
    public async Task Value_Slider_Bitmap_Respects_Current_Saturation()
    {
        var selection = new HsvColor(alpha: 1.0, hue: 30, saturation: 0.1, value: 0.8);

        using var pixels = await GenerateSliderBitmap(ColorComponent.Component3, selection);

        // Row 0 = highest Value. In HSV, chroma = V * S * 255 ≤ S * 255.
        var (r, g, b) = ReadPixel(pixels, row: 0);
        int chroma = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));

        // At S=0.1 the max possible chroma is 25.5 — near-grey for any V.
        // With perceptive S=1.0 override, chroma would be ~252.
        Assert.InRange(chroma, 0, selection.S * 255 + 10);
    }

    private static async Task<PooledList<byte>> GenerateSliderBitmap(
        ColorComponent component,
        HsvColor baseColor,
        int height = 100)
    {
        const int width = 1;
        var buffer = new PooledList<byte>(width * height * 4, ClearMode.Never, sizeToCapacity: true);

        await ColorPickerHelpers.CreateComponentBitmapAsync(
            buffer, width, height,
            Orientation.Vertical,
            ColorModel.Hsva,
            component,
            baseColor,
            isAlphaVisible: false,
            isPerceptive: false);

        return buffer;
    }

    /// <summary>
    /// Reads a pixel from a 1-pixel-wide vertical BGRA bitmap at the given row.
    /// </summary>
    private static (byte R, byte G, byte B) ReadPixel(PooledList<byte> bgraData, int row)
    {
        int offset = row * 4;
        return (R: bgraData[offset + 2], G: bgraData[offset + 1], B: bgraData[offset]);
    }
}
