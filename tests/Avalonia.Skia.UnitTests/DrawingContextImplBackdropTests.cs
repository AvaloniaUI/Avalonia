using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    // Backdrop drawing-context primitives (IDrawingContextImplWithBackdropSupport) tested in isolation on a
    // raster surface. CPU raster => deterministic, so pixels are asserted directly.
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

            // Driving the volatile backdrop primitive on a surface-less context must not throw (PDF / picture path).
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

            // A zero-radius blur yields a null filter => an empty backdrop layer that composites back as a no-op.
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

            // Blur the backdrop over the split, then draw opaque content on top.
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

            // Under a scale+translate transform the layer bounds are folded through the CTM to device pixels,
            // so a blur backdrop over a local rect covering the whole surface blurs the device split in place.
            ctx.Transform = Matrix.CreateScale(2, 2) * Matrix.CreateTranslation(3, 5);
            Assert.True(ctx.Transform.TryInvert(out var inverse));
            var destRect = new Rect(0, 0, w, h).TransformToAABB(inverse); // maps to the whole device surface

            ctx.DrawVolatileBackdropEffect(destRect, new ImmutableBlurEffect(8));
            surface.Canvas.Flush();

            // The blur landed on the right device region: mixed at the seam, dominant in each half.
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

            using var cache = ctx.CreateBackdropCache(new PixelSize(w, h));
            var destRect = new Rect(0, 0, w, h);

            // A full dirty rect refreshes the whole cache from the split, then it is drawn back through the blur.
            ctx.DrawRetainedBackdropEffect(cache, new[] { new PixelRect(0, 0, w, h) },
                new ImmutableBlurEffect(8), destRect);
            surface.Canvas.Flush();

            // A mixed seam is only possible if the cache actually captured the split before drawing it back.
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

            using var cache = ctx.CreateBackdropCache(new PixelSize(size, size));
            var destRect = new Rect(0, 0, size, size);

            // Load the cache with red from the target.
            surface.Canvas.Clear(SKColors.Red);
            surface.Canvas.Flush();
            ctx.DrawRetainedBackdropEffect(cache, new[] { new PixelRect(0, 0, size, size) },
                new ImmutableBlurEffect(0), destRect);

            // Repaint the target blue, then draw with no dirty rects: the stale (red) cache must be reused.
            surface.Canvas.Clear(SKColors.Blue);
            surface.Canvas.Flush();
            ctx.DrawRetainedBackdropEffect(cache, Array.Empty<PixelRect>(), new ImmutableBlurEffect(0), destRect);
            surface.Canvas.Flush();

            AssertColor(bitmap.GetPixel(size / 2, size / 2), 255, 0, 0); // red (refresh skipped), not blue
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

            using var cache = ctx.CreateBackdropCache(new PixelSize(w, h));
            ctx.DrawRetainedBackdropEffect(cache, new[] { new PixelRect(0, 0, w, h) },
                new ImmutableBlurEffect(0), destRect);
            surface.Canvas.Flush();

            AssertColor(bitmap.GetPixel(w / 4, h / 2), 255, 0, 0);     // refreshed + drawn back in place
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

            // A red square on a transparent target; the backdrop covers (5,5)-(25,25), the shadow is offset by 8.
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

            // Opaque backdrop pixels cover their own shadow, and the bounds' edges cast none outside.
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                AssertColor(bitmap.GetPixel(x, y), 255, 255, 255);
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
                using var cache = ctx.CreateBackdropCache(bounds.Size);
                ctx.DrawRetainedBackdropEffect(cache, new[] { bounds }, effect, destRect);
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
}
