using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal enum DXGI_MODE_ROTATION
    {
        DXGI_MODE_ROTATION_UNSPECIFIED = 0,

        DXGI_MODE_ROTATION_IDENTITY = 1,

        DXGI_MODE_ROTATION_ROTATE90 = 2,

        DXGI_MODE_ROTATION_ROTATE180 = 3,

        DXGI_MODE_ROTATION_ROTATE270 = 4,
    }
}
