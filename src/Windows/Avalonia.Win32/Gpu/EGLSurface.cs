using System;
using Avalonia.Platform;
using Avalonia.Platform.Gpu;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Gpu
{
    /// <summary>
    /// EGL surface on Win32 platform.
    /// </summary>
    public class EGLSurface : IEGLSurface
    {
        private readonly int _stencilBits;
        private readonly int _sampleCount;
        public IntPtr SurfaceHandle { get; }
        public IPlatformHandle PlatformHandle { get; }

        public EGLSurface(IntPtr surfaceHandle, IPlatformHandle platformHandle, int stencilBits, int sampleCount)
        {
            _stencilBits = stencilBits;
            _sampleCount = sampleCount;
            SurfaceHandle = surfaceHandle;
            PlatformHandle = platformHandle;
        }

        public (int width, int height) GetSize()
        {
            UnmanagedMethods.GetClientRect(PlatformHandle.Handle, out UnmanagedMethods.RECT clientSize);

            return (clientSize.right - clientSize.left, clientSize.bottom - clientSize.top);
        }

        public (int x, int y) GetDpi()
        {
            if (UnmanagedMethods.ShCoreAvailable)
            {
                var monitor = UnmanagedMethods.MonitorFromWindow(
                    PlatformHandle.Handle,
                    UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                        monitor,
                        UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out var dpix,
                        out var dpiy) == 0)
                {
                    return ((int)dpix, (int)dpiy);
                }
            }

            return (96, 96);
        }

        public FramebufferParameters GetFramebufferParameters()
        {
            var data = new int[1];
            GL.GetIntegerv(GL.FRAMEBUFFER_BINDING, data);
            var framebufferHandle = (IntPtr)data[0];
            
            return new FramebufferParameters
            {
                FramebufferHandle = framebufferHandle,
                SampleCount = _sampleCount,
                StencilBits = _stencilBits
            };
        }
    }
}