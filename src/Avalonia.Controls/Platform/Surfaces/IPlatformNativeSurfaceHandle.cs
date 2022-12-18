using System;
using Avalonia.Metadata;

namespace Avalonia.Platform.Surfaces
{
    [Unstable]
    public interface INativeHandlePlatformSurface : IPlatformHandle
    {
        PixelSize Size { get; }
        double Scaling { get; }
    }
}
