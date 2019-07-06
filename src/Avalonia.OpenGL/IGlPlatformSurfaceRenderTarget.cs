using System;

namespace Avalonia.OpenGL
{
    public interface IGlPlatformSurfaceRenderTarget : IDisposable
    {
        IGlPlatformSurfaceRenderingSession BeginDraw();
    }

    public interface IGlPlatformSurfaceRenderTargetWithCorruptionInfo : IGlPlatformSurfaceRenderTarget
    {
        bool IsCorrupted { get; }
    }
}
