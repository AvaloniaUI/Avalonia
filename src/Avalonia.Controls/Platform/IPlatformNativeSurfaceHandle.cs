using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Platform
{
    public interface IPlatformNativeSurfaceHandle : IPlatformHandle
    {
        PixelSize Size { get; }
        double Scaling { get; }

        IntPtr Display { get { return IntPtr.Zero; } }
    }
}
