using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;
using static Avalonia.Impeller.UnitTests.ImpellerTestApp;

namespace Avalonia.Impeller.UnitTests;

public class RenderTargetBitmapTests
{
    [Fact]
    public void Drawing_Is_Read_Back()
    {
        using var app = Start();
        using var bitmap = new RenderTargetBitmap(new PixelSize(20, 20));
        using (var context = bitmap.CreateDrawingContext())
            context.FillRectangle(Brushes.Red, new Rect(0, 0, 10, 10));

        var pixels = ReadPixels(bitmap);
        Assert.Equal((255, 0, 0, 255), Pixel(pixels, 20, 5, 5));
        Assert.Equal((0, 0, 0, 0), Pixel(pixels, 20, 15, 15));
    }

    [Fact]
    public void Drawing_Is_Scaled_To_Dpi()
    {
        using var app = Start();
        using var bitmap = new RenderTargetBitmap(new PixelSize(20, 20), new Vector(192, 192));
        using (var context = bitmap.CreateDrawingContext())
            context.FillRectangle(Brushes.Red, new Rect(0, 0, 5, 5));

        var pixels = ReadPixels(bitmap);
        Assert.Equal(255, Pixel(pixels, 20, 9, 9).A);
        Assert.Equal(0, Pixel(pixels, 20, 11, 11).A);
    }

    [Fact]
    public void Drawing_Without_Clear_Keeps_Content()
    {
        using var app = Start();
        using var bitmap = new RenderTargetBitmap(new PixelSize(20, 20));
        using (var context = bitmap.CreateDrawingContext())
            context.FillRectangle(Brushes.Red, new Rect(0, 0, 10, 10));
        using (var context = bitmap.CreateDrawingContext(false))
            context.FillRectangle(Brushes.Blue, new Rect(10, 10, 10, 10));

        var pixels = ReadPixels(bitmap);
        Assert.Equal((255, 0, 0, 255), Pixel(pixels, 20, 5, 5));
        Assert.Equal((0, 0, 255, 255), Pixel(pixels, 20, 15, 15));

        // Pixels that were read back are kept under the next drawing.
        using (var context = bitmap.CreateDrawingContext(false))
            context.FillRectangle(Brushes.Lime, new Rect(10, 0, 10, 10));
        pixels = ReadPixels(bitmap);
        Assert.Equal((255, 0, 0, 255), Pixel(pixels, 20, 5, 5));
        Assert.Equal((0, 255, 0, 255), Pixel(pixels, 20, 15, 5));
    }

    [Fact]
    public void Drawing_With_Clear_Replaces_Content()
    {
        using var app = Start();
        using var bitmap = new RenderTargetBitmap(new PixelSize(20, 20));
        using (var context = bitmap.CreateDrawingContext())
            context.FillRectangle(Brushes.Red, new Rect(0, 0, 10, 10));
        using (var context = bitmap.CreateDrawingContext())
            context.FillRectangle(Brushes.Blue, new Rect(10, 10, 10, 10));

        var pixels = ReadPixels(bitmap);
        Assert.Equal(0, Pixel(pixels, 20, 5, 5).A);
        Assert.Equal((0, 0, 255, 255), Pixel(pixels, 20, 15, 15));
    }

    [Fact]
    public void Bitmaps_Are_Drawn()
    {
        using var app = Start();
        using var source = new WriteableBitmap(new PixelSize(2, 2), new Vector(96, 96), PixelFormats.Rgba8888,
            AlphaFormat.Premul);
        using (var fb = source.Lock())
        {
            unsafe
            {
                var p = (uint*)fb.Address;
                // Opaque green (RGBA bytes 00 FF 00 FF).
                for (var i = 0; i < 4; i++)
                    p[i] = 0xFF00FF00;
            }
        }

        using var bitmap = new RenderTargetBitmap(new PixelSize(20, 20));
        using (var context = bitmap.CreateDrawingContext())
            context.DrawImage(source, new Rect(0, 0, 2, 2), new Rect(0, 0, 10, 10));

        var pixels = ReadPixels(bitmap);
        Assert.Equal((0, 255, 0, 255), Pixel(pixels, 20, 5, 5));
        Assert.Equal(0, Pixel(pixels, 20, 15, 15).A);
    }

    [Fact]
    public void Render_Target_Bitmaps_Can_Be_Nested()
    {
        using var app = Start();
        using var inner = new RenderTargetBitmap(new PixelSize(10, 10));
        using (var context = inner.CreateDrawingContext())
            context.FillRectangle(Brushes.Red, new Rect(0, 0, 10, 10));

        using var outer = new RenderTargetBitmap(new PixelSize(20, 20));
        using (var context = outer.CreateDrawingContext())
            context.DrawImage(inner, new Rect(0, 0, 10, 10), new Rect(10, 10, 10, 10));

        var pixels = ReadPixels(outer);
        Assert.Equal(0, Pixel(pixels, 20, 5, 5).A);
        Assert.Equal((255, 0, 0, 255), Pixel(pixels, 20, 15, 15));
    }

    [Fact]
    public void Save_Writes_A_Png()
    {
        using var app = Start();
        using var bitmap = new RenderTargetBitmap(new PixelSize(20, 20));
        using (var context = bitmap.CreateDrawingContext())
            context.FillRectangle(Brushes.Red, new Rect(0, 0, 10, 10));

        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        stream.Position = 0;
        using var loaded = new Bitmap(stream);
        var pixels = ReadPixels(loaded);
        Assert.Equal(new PixelSize(20, 20), loaded.PixelSize);
        Assert.Equal((255, 0, 0, 255), Pixel(pixels, 20, 5, 5));
        Assert.Equal(0, Pixel(pixels, 20, 15, 15).A);
    }
}
