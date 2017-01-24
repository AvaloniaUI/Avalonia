using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Platform.Surfaces
{
    public interface INativeWindowPlatformSurface
    {
        IntPtr Handle { get; }
    }

    public class NativeWindowPlatformSurface : INativeWindowPlatformSurface
    {
        public NativeWindowPlatformSurface(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }
    }
}
