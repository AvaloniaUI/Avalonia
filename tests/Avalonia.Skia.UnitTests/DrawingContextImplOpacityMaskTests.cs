using Avalonia.Media;
using Avalonia.Media.Immutable;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class DrawingContextImplOpacityMaskTests
{
    [Fact]
    public void Opacity_Mask_Below_PushOpacity_Applies_The_Opacity_Once()
    {
        Assert.InRange(DrawBlackOnWhite(opacity: 0.5, maskOpacity: 1), 122, 132);
    }

    [Fact]
    public void Opacity_Mask_Applies_Its_Own_Opacity()
    {
        Assert.InRange(DrawBlackOnWhite(opacity: 1, maskOpacity: 0.5), 122, 132);
    }

    [Fact]
    public void Opacity_Mask_Below_PushOpacity_Applies_Both_Opacities()
    {
        // 25% black over white.
        Assert.InRange(DrawBlackOnWhite(opacity: 0.5, maskOpacity: 0.5), 186, 196);
    }

    // Draws a black rectangle on white through an opacity and an opacity mask. Returns the red channel.
    private static byte DrawBlackOnWhite(double opacity, double maskOpacity)
    {
        const int size = 16;
        using var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var surface = SKSurface.Create(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes);
        using var ctx = new DrawingContextImpl(
            new DrawingContextImpl.CreateInfo { Surface = surface, Dpi = new Vector(96, 96) });

        surface.Canvas.Clear(SKColors.White);
        var bounds = new Rect(0, 0, size, size);
        ctx.PushOpacity(opacity, bounds);
        ctx.PushOpacityMask(new ImmutableSolidColorBrush(Colors.Black, maskOpacity), bounds);
        ctx.DrawRectangle(Brushes.Black, null, bounds);
        ctx.PopOpacityMask();
        ctx.PopOpacity();
        surface.Canvas.Flush();

        return bitmap.GetPixel(size / 2, size / 2).Red;
    }
}
