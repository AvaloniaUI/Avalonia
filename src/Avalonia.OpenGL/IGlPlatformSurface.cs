using System;

namespace Avalonia.OpenGL
{
    public interface IGlPlatformSurface
    {
        IGlPlatformSurfaceRenderTarget CreateGlRenderTarget();
    }

    public interface IGlPlatformSurfaceRenderTarget : IDisposable
    {
        IGlPlatformSurfaceRenderingSession BeginDraw();
    }

    public interface IGlPlatformSurfaceRenderingSession : IDisposable
    {
        IGlDisplay Display { get; }
        int PixelWidth { get; }
        int PixelHeight { get; }
        Vector Dpi { get; }
    }
}
