using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DirectX
{
    internal unsafe class DirectXUnmanagedMethods
    {
        // these return HRESULTs expecting Marshall to throw Win32 exceptions on failures
        [DllImport("dxgi", ExactSpelling = true, PreserveSig = false)]
        internal static extern void CreateDXGIFactory(ref Guid riid, out void* ppFactory);

        // these return HRESULTs expecting Marshall to throw Win32 exceptions on failures
        [DllImport("dxgi", ExactSpelling = true, PreserveSig = false)]
        internal static extern void CreateDXGIFactory1(ref Guid riid, out void* ppFactory);

        [DllImport("user32", ExactSpelling = true)]
        internal static extern bool GetMonitorInfoW(HANDLE hMonitor, IntPtr lpmi);

        [DllImport("user32", ExactSpelling = true)]
        internal static extern bool EnumDisplaySettingsW(ushort* lpszDeviceName, uint iModeNum, DEVMODEW* lpDevMode);

        [DllImport("d3d11", ExactSpelling = true, PreserveSig = false)]
        public static extern void D3D11CreateDevice(
            IntPtr adapter, D3D_DRIVER_TYPE DriverType,
            IntPtr Software,
            uint Flags,
            D3D_FEATURE_LEVEL[] pFeatureLevels,
            uint FeatureLevels,
            uint SDKVersion,
            out IntPtr ppDevice,
            out D3D_FEATURE_LEVEL pFeatureLevel,
            IntPtr* ppImmediateContext);
    }
}
