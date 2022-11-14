using System;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IPlatformNativeSurfaceHandle : IPlatformHandle
    {
        PixelSize Size { get; }
        double Scaling { get; }
    }
}
