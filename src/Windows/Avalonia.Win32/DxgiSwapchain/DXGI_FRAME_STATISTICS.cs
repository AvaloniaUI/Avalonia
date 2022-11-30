using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct DXGI_FRAME_STATISTICS
    {
        public uint PresentCount;

        public uint PresentRefreshCount;

        public uint SyncRefreshCount;

        public ulong SyncQPCTime;

        public ulong SyncGPUTime;
    }
}
