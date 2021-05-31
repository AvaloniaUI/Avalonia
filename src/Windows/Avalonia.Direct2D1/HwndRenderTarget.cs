using System.Drawing;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using Vortice.DXGI;

namespace Avalonia.Direct2D1
{
    internal class HwndRenderTarget : SwapChainRenderTarget
    {
        private readonly IPlatformHandle _window;

        public HwndRenderTarget(IPlatformHandle window)
        {
            _window = window;
        }

        protected override IDXGISwapChain1 CreateSwapChain(IDXGIFactory2 dxgiFactory, SwapChainDescription1 swapChainDesc)
        {
            return dxgiFactory.CreateSwapChainForHwnd(Direct2D1Platform.DxgiDevice, _window.Handle, swapChainDesc);
        }

        protected override SizeF GetWindowDpi()
        {
            if (UnmanagedMethods.ShCoreAvailable)
            {
                uint dpix, dpiy;

                var monitor = UnmanagedMethods.MonitorFromWindow(
                    _window.Handle,
                    UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                        monitor,
                        UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out dpix,
                        out dpiy) == 0)
                {
                    return new SizeF(dpix, dpiy);
                }
            }

            return new SizeF(96, 96);
        }

        protected override System.Drawing.Size GetWindowSize()
        {
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_window.Handle, out rc);
            return new System.Drawing.Size(rc.right - rc.left, rc.bottom - rc.top);
        }
    }
}
