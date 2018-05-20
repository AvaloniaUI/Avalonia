// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Gpu;
using Avalonia.Platform;
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

        /// <summary>
        /// Create new <see cref="EGLSurface"/> instance.
        /// </summary>
        /// <param name="surfaceHandle">Native surface handle.</param>
        /// <param name="platformHandle">Platform handle.</param>
        /// <param name="stencilBits">Surface stencil bits.</param>
        /// <param name="sampleCount">Surface sample count.</param>
        public EGLSurface(IntPtr surfaceHandle, IPlatformHandle platformHandle, int stencilBits, int sampleCount)
        {
            _stencilBits = stencilBits;
            _sampleCount = sampleCount;
            SurfaceHandle = surfaceHandle;
            PlatformHandle = platformHandle;
        }

        /// <inheritdoc />
        public (int width, int height) GetSize()
        {
            UnmanagedMethods.GetClientRect(PlatformHandle.Handle, out UnmanagedMethods.RECT clientSize);

            var width = clientSize.right - clientSize.left;
            var height = clientSize.bottom - clientSize.top;

            return (width, height);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public FramebufferParameters GetFramebufferParameters()
        {
            GL.GetIntegerv(GL.FRAMEBUFFER_BINDING, out int framebufferHandle);
            
            return new FramebufferParameters
            {
                FramebufferHandle = (IntPtr)framebufferHandle,
                SampleCount = _sampleCount,
                StencilBits = _stencilBits
            };
        }
    }
}