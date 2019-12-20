using System;
using System.Reactive.Disposables;
using System.Threading;
using static Avalonia.OpenGL.EglConsts;

namespace Avalonia.OpenGL
{
    public class EglContext : IGlContext
    {
        private readonly EglDisplay _disp;
        private readonly EglInterface _egl;
        private readonly object _lock = new object();

        public EglContext(EglDisplay display, EglInterface egl, IntPtr ctx, EglSurface offscreenSurface)
        {
            _disp = display;
            _egl = egl;
            Context = ctx;
            OffscreenSurface = offscreenSurface;
        }

        public IntPtr Context { get; }
        public EglSurface OffscreenSurface { get; }
        public IGlDisplay Display => _disp;

        public IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return Disposable.Create(() => Monitor.Exit(_lock));
        }

        class RestoreContext : IDisposable
        {
            private readonly EglInterface _egl;
            private readonly IntPtr _display;
            private IntPtr _context, _read, _draw;

            public RestoreContext(EglInterface egl)
            {
                _egl = egl;
                _display = _egl.GetCurrentDisplay();
                _context = _egl.GetCurrentContext();
                _read = _egl.GetCurrentSurface(EGL_READ);
                _draw = _egl.GetCurrentSurface(EGL_DRAW);
            }

            public void Dispose() => _egl.MakeCurrent(_display, _draw, _read, _context);
        }
        
        public IDisposable MakeCurrent()
        {
            var old = new RestoreContext(_egl);
            if (!_egl.MakeCurrent(_disp.Handle, IntPtr.Zero, IntPtr.Zero, Context))
                throw OpenGlException.GetFormattedException("eglMakeCurrent", _egl);
            return old;
        }
        
        public IDisposable MakeCurrent(EglSurface surface)
        {
            var old = new RestoreContext(_egl);
            var surf = surface ?? OffscreenSurface;
            if (!_egl.MakeCurrent(_disp.Handle, surf.DangerousGetHandle(), surf.DangerousGetHandle(), Context))
                throw OpenGlException.GetFormattedException("eglMakeCurrent", _egl);
            return old;
        }

        public void Dispose()
        {
            _egl.DestroyContext(_disp.Handle, Context);
            OffscreenSurface?.Dispose();
        }
    }
}
