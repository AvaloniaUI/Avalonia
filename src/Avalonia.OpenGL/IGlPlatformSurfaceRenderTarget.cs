using System;

namespace Avalonia.OpenGL
{
    public interface IGlPlatformSurfaceRenderTarget : IDisposable
    {
        IGlPlatformSurfaceRenderingSession BeginDraw();
    }
}