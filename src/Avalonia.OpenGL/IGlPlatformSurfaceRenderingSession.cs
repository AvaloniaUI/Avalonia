using System;

namespace Avalonia.OpenGL
{
    public interface IGlPlatformSurfaceRenderingSession : IDisposable
    {
        IGlContext Context { get; }
        PixelSize Size { get; }
        double Scaling { get; }
        bool IsYFlipped { get; }
    }
}
