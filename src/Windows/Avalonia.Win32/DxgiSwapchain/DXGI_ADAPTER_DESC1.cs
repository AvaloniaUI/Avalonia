using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct DXGI_ADAPTER_DESC1
    {
        public fixed ushort Description[128];

        public uint VendorId;

        public uint DeviceId;

        public uint SubSysId;

        public uint Revision;

        public nuint DedicatedVideoMemory;

        public nuint DedicatedSystemMemory;

        public nuint SharedSystemMemory;

        public ulong AdapterLuid;

        public uint Flags;
    }
}
