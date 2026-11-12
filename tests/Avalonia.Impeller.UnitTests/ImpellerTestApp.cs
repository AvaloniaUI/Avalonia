using System;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.UnitTests;

namespace Avalonia.Impeller.UnitTests;

/// <summary>Starts a test application on the Impeller backend.</summary>
internal static class ImpellerTestApp
{
    private static readonly Lazy<IPlatformGraphics?> s_glGraphics = new(CreateGlGraphics);

    /// <summary>Headless OpenGL ES through Mesa, or null where it isn't available.</summary>
    public static IPlatformGraphics? GlGraphics => s_glGraphics.Value;

    private static IPlatformGraphics? CreateGlGraphics()
    {
        if (!OperatingSystem.IsLinux())
            return null;
        try
        {
            return EglPlatformGraphics.TryCreate(() => new EglDisplay(new EglDisplayCreationOptions
            {
                Egl = new EglInterface(),
                PlatformType = EglConsts.EGL_PLATFORM_SURFACELESS_MESA,
                SupportsMultipleContexts = true,
                SupportsContextSharing = true,
                AllowPbufferOnlyConfigs = true,
                GlVersions = [new GlVersion(GlProfileType.OpenGLES, 3, 0)],
            }));
        }
        catch (DllNotFoundException)
        {
            return null;
        }
    }

    public static IDisposable Start()
    {
        var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(
            renderInterface: new PlatformRenderInterface(new ImpellerOptions()),
            fontManagerImpl: new FontManagerImpl()));
        if (GlGraphics is { } graphics)
            AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(graphics);
        return app;
    }

    /// <summary>Reads the pixels of a bitmap as premultiplied RGBA rows.</summary>
    public static unsafe byte[] ReadPixels(Bitmap bitmap)
    {
        var size = bitmap.PixelSize;
        var pixels = new byte[size.Width * size.Height * 4];
        using var converted = new WriteableBitmap(size, new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Premul);
        fixed (byte* p = pixels)
        {
            using (var target = converted.Lock())
                bitmap.CopyPixels(target);
            using var locked = converted.Lock();
            for (var y = 0; y < size.Height; y++)
                new ReadOnlySpan<byte>((byte*)locked.Address + y * locked.RowBytes, size.Width * 4)
                    .CopyTo(new Span<byte>(p + y * size.Width * 4, size.Width * 4));
        }
        return pixels;
    }

    public static (byte R, byte G, byte B, byte A) Pixel(byte[] pixels, int width, int x, int y)
    {
        var i = (y * width + x) * 4;
        return (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
    }
}
