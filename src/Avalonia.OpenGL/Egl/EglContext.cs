using System;
using System.Reactive.Disposables;
using System.Threading;
using static Avalonia.OpenGL.Egl.EglConsts;

namespace Avalonia.OpenGL.Egl
{
    public class EglContext : IGlContext
    {
        private readonly EglDisplay _disp;
        private readonly EglInterface _egl;
        private readonly EglContext _sharedWith;
        private readonly object _lock = new object();

        public EglContext(EglDisplay display, EglInterface egl, EglContext sharedWith, IntPtr ctx, Func<EglContext, EglSurface> offscreenSurface,
            GlVersion version, int sampleCount, int stencilSize)
        {
            _disp = display;
            _egl = egl;
            _sharedWith = sharedWith;
            Context = ctx;
            OffscreenSurface = offscreenSurface(this);
            Version = version;
            SampleCount = sampleCount;
            StencilSize = stencilSize;
            using (MakeCurrent())
                GlInterface = GlInterface.FromNativeUtf8GetProcAddress(version, _egl.GetProcAddress);
        }

        public IntPtr Context { get; }
        public EglSurface OffscreenSurface { get; }
        public GlVersion Version { get; }
        public GlInterface GlInterface { get; }
        public int SampleCount { get; }
        public int StencilSize { get; }
        public EglDisplay Display => _disp;

        class RestoreContext : IDisposable
        {
            private readonly EglInterface _egl;
            private readonly object _l;
            private readonly IntPtr _display;
            private IntPtr _context, _read, _draw;

            public RestoreContext(EglInterface egl, IntPtr defDisplay, object l)
            {
                _egl = egl;
                _l = l;
                _display = _egl.GetCurrentDisplay();
                if (_display == IntPtr.Zero)
                    _display = defDisplay;
                _context = _egl.GetCurrentContext();
                _read = _egl.GetCurrentSurface(EGL_READ);
                _draw = _egl.GetCurrentSurface(EGL_DRAW);
            }

            public void Dispose() 
            {
                _egl.MakeCurrent(_display, _draw, _read, _context);
                Monitor.Exit(_l);
            }

        }

        public IDisposable MakeCurrent() => MakeCurrent(OffscreenSurface);

        public IDisposable MakeCurrent(EglSurface surface)
        {
            Monitor.Enter(_lock);
            var success = false;
            try
            {
                var old = new RestoreContext(_egl, _disp.Handle, _lock);
                var surf = surface ?? OffscreenSurface;
                _egl.MakeCurrent(_disp.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (!_egl.MakeCurrent(_disp.Handle, surf?.DangerousGetHandle() ?? IntPtr.Zero,
                    surf?.DangerousGetHandle() ?? IntPtr.Zero, Context))
                    throw OpenGlException.GetFormattedException("eglMakeCurrent", _egl);
                success = true;
                return old;
            }
            finally
            {
                if(!success)
                    Monitor.Exit(_lock);
            }
        }
        
        public IDisposable EnsureCurrent()
        {
            if(IsCurrent)
                return Disposable.Empty;
            return MakeCurrent();
        }

        public IDisposable EnsureLocked()
        {
            if (IsCurrent)
                return Disposable.Empty;
            Monitor.Enter(_lock);
            return Disposable.Create(() => Monitor.Exit(_lock));
        }

        public bool IsSharedWith(IGlContext context)
        {
            var c = (EglContext)context;
            return c == this
                   || c._sharedWith == this
                   || _sharedWith == context
                   || _sharedWith != null && _sharedWith == c._sharedWith;
        }

        public bool IsCurrent => _egl.GetCurrentDisplay() == _disp.Handle && _egl.GetCurrentContext() == Context;

        public void Dispose()
        {
            _egl.DestroyContext(_disp.Handle, Context);
            OffscreenSurface?.Dispose();
        }
    }
}
