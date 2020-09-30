using System;

namespace Avalonia.OpenGL.Surfaces
{
    public interface IGlPlatformSurfaceRenderingSession : IDisposable
    {
        IGlContext Context { get; }
        PixelSize Size { get; }
        double Scaling { get; }
        bool IsYFlipped { get; }
    }
}
