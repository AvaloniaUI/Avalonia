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
        
        [DllImport("dxgi", ExactSpelling = true)]
        internal static extern HRESULT CreateDXGIFactory(Guid* riid, void** ppFactory);

        
        [DllImport("dxgi", ExactSpelling = true)]
        internal static extern HRESULT CreateDXGIFactory1(Guid* riid, void** ppFactory);

        [DllImport("user32", ExactSpelling = true)]
        internal static extern bool GetMonitorInfoW(HANDLE hMonitor, IntPtr lpmi);

        [DllImport("user32", ExactSpelling = true)]
        internal static extern bool EnumDisplaySettingsW(ushort* lpszDeviceName, uint iModeNum, DEVMODEW* lpDevMode);

        [DllImport("user32", ExactSpelling = true, SetLastError = true)]
        internal static extern bool GetClientRect(IntPtr hWnd, Interop.UnmanagedMethods.RECT* lpRect);
    }
}
