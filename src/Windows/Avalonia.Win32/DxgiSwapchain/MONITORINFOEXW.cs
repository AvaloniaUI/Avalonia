using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe partial struct MONITORINFOEXW
    {
        internal MONITORINFO Base;

        internal fixed ushort szDevice[32];
    }
}
