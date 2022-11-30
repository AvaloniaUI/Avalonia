using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal enum D3D11_RESOURCE_DIMENSION
    {
        D3D11_USAGE_DEFAULT = 0,

        D3D11_USAGE_IMMUTABLE = 1,

        D3D11_USAGE_DYNAMIC = 2,

        D3D11_USAGE_STAGING = 3,
    }
}
