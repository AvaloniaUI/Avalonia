using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct DXGI_GAMMA_CONTROL_CAPABILITIES
    {
        public int ScaleAndOffsetSupported;

        public float MaxConvertedValue;

        public float MinConvertedValue;

        public uint NumGammaControlPoints;

        public fixed float ControlPointPositions[1025];
    }
}
