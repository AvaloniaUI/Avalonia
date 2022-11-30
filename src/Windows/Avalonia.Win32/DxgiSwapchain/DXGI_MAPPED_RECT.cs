using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct DXGI_MAPPED_RECT
    {
        public int Pitch;

        public byte* pBits;
    }
}
