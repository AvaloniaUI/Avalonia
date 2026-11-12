using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;
using static Avalonia.Impeller.UnitTests.ImpellerTestApp;

namespace Avalonia.Impeller.UnitTests;

public class BitmapTests
{
    [Fact]
    public void Loaded_Bitmaps_Are_Drawn()
    {
        using var app = Start();
        using var source = new RenderTargetBitmap(new PixelSize(4, 4));
        using (var context = source.CreateDrawingContext())
            context.FillRectangle(Media.Brushes.Red, new Rect(0, 0, 2, 4));
        using var stream = new MemoryStream();
        source.Save(stream, PngBitmapEncoderOptions.Default);
        stream.Position = 0;
        using var loaded = new Bitmap(stream);

        using var target = new RenderTargetBitmap(new PixelSize(4, 4));
        using (var context = target.CreateDrawingContext())
            context.DrawImage(loaded, new Rect(0, 0, 4, 4));

        var pixels = ReadPixels(target);
        Assert.Equal((255, 0, 0, 255), Pixel(pixels, 4, 0, 0));
        Assert.Equal(0, Pixel(pixels, 4, 3, 3).A);
    }

    [Fact]
    public unsafe void Other_Pixel_Formats_Are_Converted()
    {
        using var app = Start();
        // Opaque blue in BGRA byte order.
        var bgra = new byte[] { 255, 0, 0, 255 };
        fixed (byte* p = bgra)
        {
            using var bitmap = new Bitmap(PixelFormats.Bgra8888, AlphaFormat.Premul, (IntPtr)p,
                new PixelSize(1, 1), new Vector(96, 96), 4);
            using var target = new RenderTargetBitmap(new PixelSize(1, 1));
            using (var context = target.CreateDrawingContext())
                context.DrawImage(bitmap, new Rect(0, 0, 1, 1));
            Assert.Equal((0, 0, 255, 255), Pixel(ReadPixels(target), 1, 0, 0));
        }
    }

    [Fact]
    public unsafe void Unpremultiplied_Writeable_Bitmaps_Draw_Correctly()
    {
        using var app = Start();
        using var source = new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96), PixelFormats.Rgba8888,
            AlphaFormat.Unpremul);
        using (var fb = source.Lock())
            *(uint*)fb.Address = 0x80FFFFFFu; // Half transparent white.

        using var target = new RenderTargetBitmap(new PixelSize(4, 4));
        using (var context = target.CreateDrawingContext())
            context.DrawImage(source, new Rect(0, 0, 1, 1), new Rect(0, 0, 4, 4));

        var pixel = Pixel(ReadPixels(target), 4, 2, 2);
        Assert.InRange(pixel.A, 126, 130);
        Assert.InRange(pixel.R, 126, 130);
    }
}
