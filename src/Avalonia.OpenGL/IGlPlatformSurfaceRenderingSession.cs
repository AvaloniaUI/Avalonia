using System;

namespace Avalonia.OpenGL
{
    public interface IGlPlatformSurfaceRenderingSession : IDisposable
    {
        IGlDisplay Display { get; }
        PixelSize Size { get; }
        double Scaling { get; }
    }
}
