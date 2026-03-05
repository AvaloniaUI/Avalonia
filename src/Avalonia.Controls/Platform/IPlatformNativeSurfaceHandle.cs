using System;
using Avalonia.Platform.Surfaces;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface INativePlatformHandleSurface : IPlatformHandle, IPlatformRenderSurface
    {
        PixelSize Size { get; }
        double Scaling { get; }
    }
}
