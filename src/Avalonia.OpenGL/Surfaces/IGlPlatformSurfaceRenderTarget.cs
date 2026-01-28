using System;

namespace Avalonia.OpenGL.Surfaces
{
    public interface IGlPlatformSurfaceRenderTarget : IDisposable
    {
        bool IsCorrupted { get; }

        IGlPlatformSurfaceRenderingSession BeginDraw(PixelSize? expectedPixelSize);
    }
}
