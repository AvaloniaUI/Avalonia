// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Logging;
using Avalonia.OpenGL;
using System;

namespace Avalonia.Win32.EGL
{
    public class EglContext : IGlContext
    {
        private IntPtr _context = IntPtr.Zero;
        private IntPtr _display = IntPtr.Zero;
        private IntPtr _configId = IntPtr.Zero;

        public IGlSurface Surface { get; private set; }

        /// <inheritdoc />
        public EglContext(IntPtr display, IntPtr configId, IGlSurface surface, IntPtr context)
        {
            _display = display;
            _configId = configId;
            Surface = surface;
            _context = context;
        }

        /// <inheritdoc />
        public IntPtr GetProcAddress(string functionName)
        {
            return EGL.GetProcAddress(functionName);
        }

        /// <inheritdoc />
        public bool IsCurrentContext()
        {
            return EGL.GetCurrentContext() == _context;
        }

        /// <inheritdoc />
        public void MakeCurrent()
        {
            EGL.MakeCurrent(_display, Surface.SurfaceHandle, Surface.SurfaceHandle, _context);
        }

        /// <inheritdoc />
        public void SwapBuffers()
        {
            EGL.SwapBuffers(_display, Surface.SurfaceHandle);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Surface.Dispose();
        }

        /// <inheritdoc />
        public void RecreateSurface()
        {
            EGL.MakeCurrent(_display, (IntPtr)EGL.NO_SURFACE, (IntPtr)EGL.NO_SURFACE, _context);
            if (!EGL.DestroySurface(_display, Surface.SurfaceHandle))
            {
                var error = EGL.GetError();
                Logger.Warning(LogArea.Visual, this, "Failed to destroy EGL surface with handle {handle}. Error: {error}", Surface.SurfaceHandle, error);
            }

            // TODO: More surface attributes?
            var attributes = new int[]
            {
                EGL.NONE
            };

            var newSurface = EGL.CreateWindowSurface(_display, _configId, ((EglSurface)this.Surface).PlatformHandle.Handle, attributes);
            if (newSurface == IntPtr.Zero)
            {
                var error = EGL.GetError();
                Logger.Warning(LogArea.Visual, this, "Failed to create EGL surface. Error: {error}", error);
            }

            this.Surface = new EglSurface(newSurface, ((EglSurface)this.Surface).PlatformHandle);
            EGL.MakeCurrent(_display, this.Surface.SurfaceHandle, this.Surface.SurfaceHandle, _context);
        }
    }
}