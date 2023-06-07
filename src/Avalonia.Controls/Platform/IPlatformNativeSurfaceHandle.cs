using System;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface INativePlatformHandleSurface : IPlatformHandle
    {
        PixelSize Size { get; }
        double Scaling { get; }
    }
}
