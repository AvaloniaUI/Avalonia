using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe partial struct DXGI_OUTPUT_DESC
    {
        internal fixed ushort DeviceName[32];

        internal RECT DesktopCoordinates;

        internal bool AttachedToDesktop;

        internal DXGI_MODE_ROTATION Rotation;

        internal HANDLE Monitor;
    }
}
