using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct DXGI_SWAP_CHAIN_FULLSCREEN_DESC
    {
        public DXGI_RATIONAL RefreshRate;

        public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;

        public DXGI_MODE_SCALING Scaling;

        public int Windowed;
    }
}
