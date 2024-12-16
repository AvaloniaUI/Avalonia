using System;
using Avalonia.Metadata;

namespace Avalonia.OpenGL.Surfaces
{
    public interface IGlPlatformSurfaceRenderTarget : IDisposable
    {
        IGlPlatformSurfaceRenderingSession BeginDraw();
    }

    public interface IGlPlatformSurfaceRenderTargetWithCorruptionInfo : IGlPlatformSurfaceRenderTarget
    {
        bool IsCorrupted { get; }
    }

    [PrivateApi]
    public interface IGlPlatformSurfaceRenderTarget2 : IGlPlatformSurfaceRenderTargetWithCorruptionInfo
    {
        IGlPlatformSurfaceRenderingSession BeginDraw(PixelSize expectedPixelSize);
    }

}
