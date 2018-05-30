// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using System;

namespace Avalonia.Win32.EGL
{
    public class EglSurface : IGlSurface, IDisposable
    {
        public IntPtr SurfaceHandle { get; }
        public IPlatformHandle PlatformHandle { get; set; }

        public EglSurface(IntPtr surfaceHandle, IPlatformHandle platformHandle)
        {
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

        public void Dispose()
        {
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
    }
}