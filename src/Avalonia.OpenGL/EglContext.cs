using System;
using System.Reactive.Disposables;
using System.Threading;

namespace Avalonia.OpenGL
{
    public class EglContext : IGlContext
    {
        private readonly EglDisplay _disp;
        private readonly EglInterface _egl;
        private readonly object _lock = new object();

        public EglContext(EglDisplay display, EglInterface egl, IntPtr ctx, IntPtr offscreenSurface)
        {
            _disp = display;
            _egl = egl;
            Context = ctx;
            OffscreenSurface = offscreenSurface;
        }

        public IntPtr Context { get; }
        public IntPtr OffscreenSurface { get; }
        public IGlDisplay Display => _disp;

        public IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return Disposable.Create(() => Monitor.Exit(_lock));
        }

        public void MakeCurrent()
        {
            if (!_egl.MakeCurrent(_disp.Handle, IntPtr.Zero, IntPtr.Zero, Context))
                throw new OpenGlException("eglMakeCurrent failed");
        }
        
        public void MakeCurrent(EglSurface surface)
        {
            var surf = ((EglSurface)surface)?.DangerousGetHandle() ?? OffscreenSurface;
            if (!_egl.MakeCurrent(_disp.Handle, surf, surf, Context))
                throw new OpenGlException("eglMakeCurrent failed");
        }
    }
}
