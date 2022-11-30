using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct DXGI_SURFACE_DESC
    {
        public uint Width;

        public uint Height;

        public DXGI_FORMAT Format;

        public DXGI_SAMPLE_DESC SampleDesc;
    }
}
