using System;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;
using Xunit;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Impeller.UnitTests;

public class SkiaLeaseTests
{
    private const int Size = 32;

    // Draws into an OpenGL layer through the lease and reads the layer back.
    private static byte[] DrawThroughLease(Action<ISkiaSharpApiLease> draw)
    {
        var graphics = ImpellerTestApp.GlGraphics;
        Assert.SkipWhen(graphics is null, "Needs headless OpenGL ES.");
        using var app = ImpellerTestApp.Start();
        using var glContext = (IGlContext)graphics!.CreateContext();
        using var current = glContext.EnsureCurrent();
        using var backend = ImpellerGlBackendContext.Create(glContext);
        using var layer = backend.CreateTextureLayer(new PixelSize(Size, Size), new Vector(96, 96));

        // Impeller renders a frame first, so the lease starts from Impeller's GL state.
        using (var context = layer.CreateDrawingContext())
        {
            context.DrawRectangle(Brushes.Blue, null, new RoundedRect(new Rect(0, 0, 4, 4), 2));
            context.PushOpacity(0.5, null);
            context.DrawEllipse(Brushes.Blue, null, new Rect(0, 0, 3, 3));
            context.PopOpacity();
        }

        using (var context = layer.CreateDrawingContext())
        {
            var feature = (ISkiaSharpApiLeaseFeature?)context.GetFeature(typeof(ISkiaSharpApiLeaseFeature));
            Assert.NotNull(feature);
            using var lease = feature.Lease();
            draw(lease);
        }

        using var image = layer.Snapshot();
        var pixels = new byte[Size * Size * 4];
        unsafe
        {
            fixed (byte* p = pixels)
                Assert.True(image.ReadPixels(new SKImageInfo(Size, Size, SKColorType.Rgba8888, SKAlphaType.Premul),
                    (IntPtr)p, Size * 4));
        }
        return pixels;
    }

    private static (byte R, byte G, byte B, byte A) Pixel(byte[] pixels, int x, int y) =>
        ImpellerTestApp.Pixel(pixels, Size, x, y);

    [Fact]
    public void SkCanvas_Drawing_Is_Composited()
    {
        var pixels = DrawThroughLease(lease =>
        {
            using var paint = new SKPaint { Color = SKColors.Red };
            lease.SkCanvas.DrawRect(new SKRect(0, 0, 16, 16), paint);
        });

        Assert.Equal((255, 0, 0, 255), Pixel(pixels, 8, 8));
        Assert.Equal(0, Pixel(pixels, 24, 24).A);
    }

    [Fact]
    public void Platform_Graphics_Api_Can_Be_Leased()
    {
        var pixels = DrawThroughLease(lease =>
        {
            SKImage? image;
            using (var api = lease.TryLeasePlatformGraphicsApi())
            {
                var gl = Assert.IsAssignableFrom<IGlContext>(api?.Context).GlInterface;
                gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var oldFbo);

                // Render green into a texture with raw GL, like OpenGlLeasePage does.
                var texture = gl.GenTexture();
                gl.BindTexture(GL_TEXTURE_2D, texture);
                gl.TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, 16, 16, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
                var fbo = gl.GenFramebuffer();
                gl.BindFramebuffer(GL_FRAMEBUFFER, fbo);
                gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, texture, 0);
                var depth = gl.GenRenderbuffer();
                gl.BindRenderbuffer(GL_RENDERBUFFER, depth);
                gl.RenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH_COMPONENT16, 16, 16);
                gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, depth);
                gl.Viewport(0, 0, 16, 16);
                gl.ClearColor(0, 1, 0, 1);
                gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
                // Leave state behind the way GL rendering code does.
                gl.Enable(GL_DEPTH_TEST);
                gl.BindVertexArray(gl.GenVertexArray());
                gl.Flush();

                lease.GrContext!.ResetContext();
                using var backendTexture = new GRBackendTexture(16, 16, false,
                    new GRGlTextureInfo(GL_TEXTURE_2D, (uint)texture, SKColorType.Rgba8888.ToGlSizedFormat()));
                using var surface = SKSurface.Create(lease.GrContext, backendTexture, GRSurfaceOrigin.BottomLeft,
                    SKColorType.Rgba8888);
                image = surface!.Snapshot();
                lease.GrContext.Flush();

                gl.BindFramebuffer(GL_FRAMEBUFFER, oldFbo);
                gl.DeleteFramebuffer(fbo);
            }

            using (image)
                lease.SkCanvas.DrawImage(image, new SKRect(16, 16, 32, 32), default(SKSamplingOptions));
        });

        Assert.Equal(0, Pixel(pixels, 8, 8).A);
        Assert.Equal((0, 255, 0, 255), Pixel(pixels, 24, 24));
    }
}
