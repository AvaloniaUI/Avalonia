using System;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;
using Xunit;

namespace Avalonia.Impeller.UnitTests;

public class GlTests
{
    private const int Size = 20;

    private static void WithGlBackend(Action<ImpellerGlBackendContext> action)
    {
        var graphics = ImpellerTestApp.GlGraphics;
        Assert.SkipWhen(graphics is null, "Needs headless OpenGL ES through Mesa.");
        using var app = ImpellerTestApp.Start();
        using var glContext = (IGlContext)graphics!.CreateContext();
        using var current = glContext.EnsureCurrent();
        using var backend = ImpellerGlBackendContext.Create(glContext);
        action(backend);
    }

    private static SKColor Pixel(IImpellerOffscreenLayer layer, int x, int y)
    {
        using var image = layer.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }

    [Fact]
    public void Layers_Keep_Content_Between_Drawings()
    {
        WithGlBackend(backend =>
        {
            using var layer = backend.CreateTextureLayer(new PixelSize(Size, Size), new Vector(96, 96));
            using (var context = layer.CreateDrawingContext())
                context.DrawRectangle(Brushes.Red, null, new RoundedRect(new Rect(0, 0, 10, 10)));
            using (var context = layer.CreateDrawingContext())
                context.DrawRectangle(Brushes.Blue, null, new RoundedRect(new Rect(10, 10, 10, 10)));

            Assert.Equal(SKColors.Red, Pixel(layer, 5, 5));
            Assert.Equal(SKColors.Blue, Pixel(layer, 15, 15));
            Assert.Equal(0, Pixel(layer, 15, 5).Alpha);
        });
    }

    [Fact]
    public void Layers_Are_Sampled_Upright()
    {
        WithGlBackend(backend =>
        {
            using var source = backend.CreateTextureLayer(new PixelSize(Size, Size), new Vector(96, 96));
            using (var context = source.CreateDrawingContext())
                context.DrawRectangle(Brushes.Red, null, new RoundedRect(new Rect(0, 0, Size, 5)));

            using var target = backend.CreateTextureLayer(new PixelSize(Size, Size), new Vector(96, 96));
            using (var context = target.CreateDrawingContext())
                context.DrawBitmap(source, 1, new Rect(0, 0, Size, Size), new Rect(0, 0, Size, Size));

            Assert.Equal(SKColors.Red, Pixel(target, 10, 2));
            Assert.Equal(0, Pixel(target, 10, 17).Alpha);
        });
    }

    [Fact]
    public void SkiaSharp_Lease_Is_Composited()
    {
        WithGlBackend(backend =>
        {
            using var layer = backend.CreateTextureLayer(new PixelSize(Size, Size), new Vector(96, 96));
            using (var context = layer.CreateDrawingContext())
            {
                var feature = (ISkiaSharpApiLeaseFeature?)context.GetFeature(typeof(ISkiaSharpApiLeaseFeature));
                Assert.NotNull(feature);
                using var lease = feature.Lease();
                using var paint = new SKPaint { Color = SKColors.Red };
                lease.SkCanvas.DrawRect(0, 0, 10, 10, paint);
            }

            Assert.Equal(SKColors.Red, Pixel(layer, 5, 5));
            Assert.Equal(0, Pixel(layer, 15, 15).Alpha);
        });
    }
}
