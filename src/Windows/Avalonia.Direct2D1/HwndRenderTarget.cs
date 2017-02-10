using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using SharpDX;
using SharpDX.DXGI;

namespace Avalonia.Direct2D1
{
    class HwndRenderTarget : SwapChainRenderTarget
    {
        private readonly IPlatformWindowRenderSurface _window;
        private readonly AutoResetEvent _targetLock = new AutoResetEvent(true);

        public HwndRenderTarget(IPlatformWindowRenderSurface window)
        {
            _window = window;
            _window.Disposed.Register(() => _targetLock.WaitOne());
        }

        class TargetLockDisposable : IDisposable
        {
            private readonly AutoResetEvent _ev;

            public TargetLockDisposable(AutoResetEvent ev)
            {
                _ev = ev;
            }

            public void Dispose()
            {
                _ev.Set();
            }
        }

        protected override IDisposable LockTarget()
        {
            _targetLock.Reset();
            Thread.MemoryBarrier();
            if (_window.Disposed.IsCancellationRequested)
            {
                _targetLock.Set();
                throw new RenderTargetUnavailableException();
            }
            return new TargetLockDisposable(_targetLock);
        }

        public override void Dispose()
        {
            _targetLock.Dispose();
            base.Dispose();
        }

        protected override SwapChain1 CreateSwapChain(Factory2 dxgiFactory, SwapChainDescription1 swapChainDesc)
        {
            return new SwapChain1(dxgiFactory, DxgiDevice, _window.Handle, ref swapChainDesc);
        }

        protected override Size2F GetWindowDpi()
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
                    return new Size2F(dpix, dpiy);
                }
            }

            return new Size2F(96, 96);
        }

        protected override Size2 GetWindowSize()
        {
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_window.Handle, out rc);
            return new Size2(rc.right - rc.left, rc.bottom - rc.top);
        }
    }
}
