using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
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

        [DllImport("user32", ExactSpelling = true, SetLastError = true)]
        internal static extern bool GetClientRect(IntPtr hWnd, Interop.UnmanagedMethods.RECT* lpRect);
    }
}
