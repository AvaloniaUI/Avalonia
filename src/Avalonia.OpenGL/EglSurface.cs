using System;
using System.Runtime.InteropServices;

namespace Avalonia.OpenGL
{
    public class EglSurface : SafeHandle
    {
        private readonly EglDisplay _display;
        private readonly EglInterface _egl;

        public EglSurface(EglDisplay display, EglInterface egl, IntPtr surface)  : base(surface, true)
        {
            _display = display;
            _egl = egl;
        }

        protected override bool ReleaseHandle()
        {
            _egl.DestroySurface(_display.Handle, handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public IGlDisplay Display => _display;
        public void SwapBuffers() => _egl.SwapBuffers(_display.Handle, handle);
    }
}