using System;

namespace Avalonia.Platform
{
    public interface IPlatformNativeSurfaceHandle : IPlatformHandle
    {
        PixelSize Size { get; }
        double Scaling { get; }

        IntPtr Display { get { return IntPtr.Zero; } }
    }
}
