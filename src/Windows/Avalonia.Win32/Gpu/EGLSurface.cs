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
        public IntPtr SurfaceHandle { get; }
        public IPlatformHandle PlatformHandle { get; }

        public EGLSurface(IntPtr surfaceHandle, IPlatformHandle platformHandle)
        {
            SurfaceHandle = surfaceHandle;
            PlatformHandle = platformHandle;
        }

        public (int width, int height) GetSize()
        {
            UnmanagedMethods.GetClientRect(PlatformHandle.Handle, out UnmanagedMethods.RECT clientSize);

            return (clientSize.right - clientSize.left, clientSize.bottom - clientSize.top);
        }

        public FramebufferParameters GetFramebufferParameters()
        {
            var data = new int[1];
            GL.GetIntegerv(GL.FRAMEBUFFER_BINDING, data);

            // TODO: Sample and stencil bits

            return new FramebufferParameters
            {
                FramebufferHandle = (IntPtr)data[0],
                SampleCount = 0,
                StencilBits = 0
            };
        }
    }
}