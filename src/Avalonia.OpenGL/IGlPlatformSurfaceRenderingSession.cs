using System;

namespace Avalonia.OpenGL
{
    public interface IGlPlatformSurfaceRenderingSession : IDisposable
    {
        IGlDisplay Display { get; }
        // TODO: Change to PixelSize struct once https://github.com/AvaloniaUI/Avalonia/pull/1889 is merged
        System.Drawing.Size PixelSize { get; }
        double Scaling { get; }
    }
}