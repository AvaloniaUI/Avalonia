using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests;

// IDrawingContextImplWithBackdropSupport on a CPU surface, without the compositor. CPU rendering is
// deterministic, so the tests check pixels directly.
public class DrawingContextImplBackdropTests
{
    [Fact]
    public void SupportsBackdrop_Is_True_For_Surface_Backed_Context()
    {
        using var bitmap = CreateBitmap(16, 16);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        Assert.True(ctx.SupportsBackdrop);
    }

    [Fact]
    public void SupportsBackdrop_Is_False_For_Surface_Less_Context()
    {
        using var canvasBitmap = new SKBitmap(16, 16);
        using var canvas = new SKCanvas(canvasBitmap);
        var ctx = (DrawingContextImpl)DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96));

        Assert.False(ctx.SupportsBackdrop);

        // No exception on a context without a surface (as for PDF or picture recording).
        var ex = Record.Exception(() =>
        {
            ctx.DrawVolatileBackdropEffect(new Rect(0, 0, 16, 16), new ImmutableBlurEffect(4));
        });
        Assert.Null(ex);
    }

    [Fact]
    public void DrawVolatileBackdropEffect_With_Null_Filter_Preserves_Pattern()
    {
        const int w = 32, h = 16;
        using var bitmap = CreateBitmap(w, h);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        DrawSplit(surface, w, h); // red left, blue right

        // A zero blur radius gives no filter: nothing is drawn.
        ctx.DrawVolatileBackdropEffect(new Rect(0, 0, w, h), new ImmutableBlurEffect(0));
        surface.Canvas.Flush();

        AssertColor(bitmap.GetPixel(w / 4, h / 2), 255, 0, 0);     // still red
        AssertColor(bitmap.GetPixel(3 * w / 4, h / 2), 0, 0, 255); // still blue
    }

    [Fact]
    public void DrawVolatileBackdropEffect_Blur_Blends_Across_Edge_And_Content_Draws_On_Top()
    {
        const int w = 80, h = 16;
        using var bitmap = CreateBitmap(w, h);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        DrawSplit(surface, w, h);

        // Blur the backdrop (the split), then draw opaque content on top.
        ctx.DrawVolatileBackdropEffect(new Rect(0, 0, w, h), new ImmutableBlurEffect(8));
        using (var green = new SKPaint { Color = SKColors.Green })
            surface.Canvas.DrawRect(new SKRect(2, 2, 8, 8), green);
        surface.Canvas.Flush();

        // The content sits on top of the blurred backdrop.
        AssertColor(bitmap.GetPixel(5, 5), 0, 128, 0);

        // At the seam the blur mixes red and blue.
        var seam = bitmap.GetPixel(w / 2, h / 2);
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);

        // Well away from the seam it stays (nearly) pure.
        var left = bitmap.GetPixel(w / 4, h / 2);
        Assert.True(left.Red > 200 && left.Blue < 40, $"left={left}");
        var right = bitmap.GetPixel(3 * w / 4, h / 2);
        Assert.True(right.Blue > 200 && right.Red < 40, $"right={right}");
    }

    [Fact]
    public void DrawVolatileBackdropEffect_Honors_Transform()
    {
        const int w = 32, h = 16;
        using var bitmap = CreateBitmap(w, h);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        DrawSplit(surface, w, h); // drawn in device space (identity canvas)

        // With a scale and translate transform, bounds that cover the whole surface blur the split where it
        // is.
        ctx.Transform = Matrix.CreateScale(2, 2) * Matrix.CreateTranslation(3, 5);
        Assert.True(ctx.Transform.TryInvert(out var inverse));
        var destRect = new Rect(0, 0, w, h).TransformToAABB(inverse); // the whole surface

        ctx.DrawVolatileBackdropEffect(destRect, new ImmutableBlurEffect(8));
        surface.Canvas.Flush();

        // The blur is in the right place: mixed at the seam, mostly one colour in each half.
        var seam = bitmap.GetPixel(w / 2, h / 2);
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);
        var left = bitmap.GetPixel(w / 4, h / 2);
        Assert.True(left.Red > 150 && left.Blue < 90, $"left={left}");
        var right = bitmap.GetPixel(3 * w / 4, h / 2);
        Assert.True(right.Blue > 150 && right.Red < 90, $"right={right}");
    }

    [Fact]
    public void DrawRetainedBackdropEffect_Refreshes_Cache_And_Blurs()
    {
        const int w = 80, h = 16;
        using var bitmap = CreateBitmap(w, h);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        DrawSplit(surface, w, h);

        using var snapshot = ctx.CreateBackdropSnapshot(new PixelSize(w, h));
        var destRect = new Rect(0, 0, w, h);

        // A dirty rect over everything refreshes the whole snapshot from the split. Then the snapshot is drawn
        // through the blur.
        ctx.DrawRetainedBackdropEffect(snapshot, new PixelRect(0, 0, w, h), new[] { new PixelRect(0, 0, w, h) },
            new ImmutableBlurEffect(8), destRect);
        surface.Canvas.Flush();

        // The seam can only be mixed if the snapshot had the split in it.
        var seam = bitmap.GetPixel(w / 2, h / 2);
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);
        var left = bitmap.GetPixel(w / 4, h / 2);
        Assert.True(left.Red > 200 && left.Blue < 40, $"left={left}");
        var right = bitmap.GetPixel(3 * w / 4, h / 2);
        Assert.True(right.Blue > 200 && right.Red < 40, $"right={right}");
    }

    [Fact]
    public void DrawRetainedBackdropEffect_Empty_DirtyRects_Skips_Refresh()
    {
        const int size = 16;
        using var bitmap = CreateBitmap(size, size);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        using var snapshot = ctx.CreateBackdropSnapshot(new PixelSize(size, size));
        var destRect = new Rect(0, 0, size, size);

        // Refresh the snapshot with red.
        surface.Canvas.Clear(SKColors.Red);
        surface.Canvas.Flush();
        var cacheBounds = new PixelRect(0, 0, size, size);
        ctx.DrawRetainedBackdropEffect(snapshot, cacheBounds, new[] { cacheBounds },
            new ImmutableBlurEffect(0), destRect);

        // Make the surface blue, then draw without dirty rects: the snapshot is still red.
        surface.Canvas.Clear(SKColors.Blue);
        surface.Canvas.Flush();
        ctx.DrawRetainedBackdropEffect(snapshot, cacheBounds, Array.Empty<PixelRect>(), new ImmutableBlurEffect(0),
            destRect);
        surface.Canvas.Flush();

        AssertColor(bitmap.GetPixel(size / 2, size / 2), 255, 0, 0); // red: no refresh
    }

    [Fact]
    public void DrawRetainedBackdropEffect_Honors_Transform()
    {
        const int w = 32, h = 16;
        using var bitmap = CreateBitmap(w, h);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        DrawSplit(surface, w, h); // device space

        ctx.Transform = Matrix.CreateScale(2, 2) * Matrix.CreateTranslation(3, 5);
        Assert.True(ctx.Transform.TryInvert(out var inverse));
        var destRect = new Rect(0, 0, w, h).TransformToAABB(inverse);

        using var snapshot = ctx.CreateBackdropSnapshot(new PixelSize(w, h));
        ctx.DrawRetainedBackdropEffect(snapshot, new PixelRect(0, 0, w, h), new[] { new PixelRect(0, 0, w, h) },
            new ImmutableBlurEffect(0), destRect);
        surface.Canvas.Flush();

        AssertColor(bitmap.GetPixel(w / 4, h / 2), 255, 0, 0);     // refreshed and drawn in place
        AssertColor(bitmap.GetPixel(3 * w / 4, h / 2), 0, 0, 255);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropShadow_Backdrop_Shadows_Translucent_Backdrop_Within_Bounds(bool retained)
    {
        const int size = 40;
        using var bitmap = CreateBitmap(size, size);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        // A red square on a transparent surface. The backdrop bounds are (5,5)-(25,25), the shadow is offset
        // by 8.
        surface.Canvas.Clear(SKColors.Transparent);
        using (var p = new SKPaint { Color = SKColors.Red })
            surface.Canvas.DrawRect(new SKRect(10, 10, 20, 20), p);
        surface.Canvas.Flush();

        DrawBackdrop(ctx, retained, new ImmutableDropShadowEffect(8, 8, 0, Colors.Black, 1),
            new PixelRect(5, 5, 20, 20));

        AssertColor(bitmap.GetPixel(15, 15), 255, 0, 0); // the square stays on top of its shadow
        var shadow = bitmap.GetPixel(23, 23);
        Assert.True(shadow.Alpha == 255 && shadow.Red < 10, $"shadow={shadow}");
        Assert.Equal(0, bitmap.GetPixel(27, 27).Alpha); // the shadow is clipped to the backdrop bounds
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropShadow_Backdrop_Is_Invisible_Over_Opaque_Backdrop(bool retained)
    {
        const int size = 40;
        using var bitmap = CreateBitmap(size, size);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.Flush();

        DrawBackdrop(ctx, retained, new ImmutableDropShadowEffect(8, 8, 2, Colors.Black, 1),
            new PixelRect(5, 5, 20, 20));

        // Opaque backdrop pixels cover their own shadow, and the edges of the bounds cast no shadow outside.
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
            AssertColor(bitmap.GetPixel(x, y), 255, 255, 255);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Backdrop_Honors_Current_Opacity(bool retained)
    {
        const int w = 80, h = 16;
        using var bitmap = CreateBitmap(w, h);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        DrawSplit(surface, w, h); // red left, blue right

        ctx.PushOpacity(0.5, null);
        DrawBackdrop(ctx, retained, new ImmutableBlurEffect(8), new PixelRect(0, 0, w, h));
        ctx.PopOpacity();

        // Just left of the seam the blur is about half red, half blue. At 50% opacity it is mixed equally with
        // the red that is already there.
        var c = bitmap.GetPixel(w / 2 - 1, h / 2);
        Assert.InRange((int)c.Red, 170, 215);
        Assert.InRange((int)c.Blue, 40, 85);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Backdrop_Does_Not_Read_Outside_Its_Bounds(bool retained)
    {
        const int w = 80, h = 40;
        using var bitmap = CreateBitmap(w, h);
        using var surface = CreateSurface(bitmap);
        using var ctx = CreateContext(surface);

        // Red up to the left edge of the backdrop bounds, white inside of them.
        surface.Canvas.Clear(SKColors.White);
        using (var p = new SKPaint { Color = SKColors.Red })
            surface.Canvas.DrawRect(new SKRect(0, 0, 40, h), p);
        surface.Canvas.Flush();

        DrawBackdrop(ctx, retained, new ImmutableBlurEffect(8), new PixelRect(40, 10, 30, 20));

        AssertColor(bitmap.GetPixel(39, 20), 255, 0, 0);     // outside: not changed
        AssertColor(bitmap.GetPixel(40, 20), 255, 255, 255); // inside: no red from outside
    }

    [Fact]
    public void Volatile_And_Retained_Backdrops_Match_At_The_Bounds_Edge()
    {
        const int w = 80, h = 40;
        var bounds = new PixelRect(40, 10, 30, 20);

        SKBitmap Render(bool retained)
        {
            var bitmap = CreateBitmap(w, h);
            using var surface = CreateSurface(bitmap);
            using var ctx = CreateContext(surface);

            // 2px black/white stripes. Both modes see the edge pixels repeated outside of the bounds.
            surface.Canvas.Clear(SKColors.White);
            using (var p = new SKPaint { Color = SKColors.Black })
                for (var x = 0; x < w; x += 4)
                    surface.Canvas.DrawRect(new SKRect(x, 0, x + 2, h), p);
            surface.Canvas.Flush();

            DrawBackdrop(ctx, retained, new ImmutableBlurEffect(8), bounds);
            return bitmap;
        }

        using var volatileResult = Render(false);
        using var retainedResult = Render(true);

        for (var y = bounds.Y; y < bounds.Bottom; y++)
        for (var x = bounds.X; x < bounds.Right; x++)
        {
            var expected = volatileResult.GetPixel(x, y);
            AssertColor(retainedResult.GetPixel(x, y), expected.Red, expected.Green, expected.Blue);
        }
    }

    [Theory]
    [InlineData(10.0, 11.0)] // Ceiling(10)+1
    [InlineData(5.5, 7.0)]   // Ceiling(5.5)+1
    [InlineData(0.0, 0.0)]   // radius <= 0 -> 0
    public void GetBackdropSamplingRadius_Maps_Blur_To_Padding(double radius, double expected)
    {
        Assert.Equal(expected, new ImmutableBlurEffect(radius).GetBackdropSamplingRadius());
    }

    [Fact]
    public void GetBackdropSamplingRadius_Of_Null_Is_Zero()
    {
        Assert.Equal(0d, ((IEffect?)null).GetBackdropSamplingRadius());
    }

    private static void DrawBackdrop(DrawingContextImpl ctx, bool retained, IEffect effect, PixelRect bounds)
    {
        var destRect = bounds.ToRect(1);
        if (retained)
        {
            using var snapshot = ctx.CreateBackdropSnapshot(bounds.Size);
            ctx.DrawRetainedBackdropEffect(snapshot, bounds, new[] { bounds }, effect, destRect);
        }
        else
            ctx.DrawVolatileBackdropEffect(destRect, effect);

        ctx.Canvas.Flush();
    }

    private static SKBitmap CreateBitmap(int w, int h) =>
        new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul));

    private static SKSurface CreateSurface(SKBitmap bitmap) =>
        SKSurface.Create(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes);

    private static DrawingContextImpl CreateContext(SKSurface surface) =>
        new DrawingContextImpl(new DrawingContextImpl.CreateInfo { Surface = surface, Dpi = new Vector(96, 96) });

    private static void DrawSplit(SKSurface surface, int w, int h)
    {
        surface.Canvas.Clear(SKColors.Red);
        using (var p = new SKPaint { Color = SKColors.Blue })
            surface.Canvas.DrawRect(new SKRect(w / 2, 0, w, h), p);
        surface.Canvas.Flush();
    }

    private static void AssertColor(SKColor actual, byte r, byte g, byte b, int tolerance = 4)
    {
        Assert.InRange(actual.Red, Clamp(r - tolerance), Clamp(r + tolerance));
        Assert.InRange(actual.Green, Clamp(g - tolerance), Clamp(g + tolerance));
        Assert.InRange(actual.Blue, Clamp(b - tolerance), Clamp(b + tolerance));

        static byte Clamp(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
    }
}
