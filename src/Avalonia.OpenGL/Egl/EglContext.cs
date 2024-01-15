using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Reactive;
using static Avalonia.OpenGL.Egl.EglConsts;

namespace Avalonia.OpenGL.Egl
{
    public class EglContext : IGlContext
    {
        private readonly EglDisplay _disp;
        private readonly EglInterface _egl;
        private readonly EglContext? _sharedWith;
        private bool _isLost;
        private IntPtr _context;
        private readonly Action? _disposeCallback;
        private readonly Dictionary<Type, object> _features;
        private readonly object _lock;

        internal EglContext(EglDisplay display, EglInterface egl, EglContext? sharedWith, IntPtr ctx, EglSurface? offscreenSurface,
            GlVersion version, int sampleCount, int stencilSize, Action? disposeCallback,
            Dictionary<Type, Func<EglContext, object>> features)
        {
            _disp = display;
            _egl = egl;
            _sharedWith = sharedWith;
            _context = ctx;
            _disposeCallback = disposeCallback;
            OffscreenSurface = offscreenSurface;
            Version = version;
            SampleCount = sampleCount;
            StencilSize = stencilSize;
            _lock = display.ContextSharedSyncRoot ?? new object();
            using (MakeCurrent())
            {
                GlInterface = GlInterface.FromNativeUtf8GetProcAddress(version, _egl.GetProcAddress);
                _features = features.ToDictionary(x => x.Key, x => x.Value(this));
            }
        }

        public IntPtr Context =>
            _context == IntPtr.Zero ? throw new ObjectDisposedException(nameof(EglContext)) : _context; 
        public EglSurface? OffscreenSurface { get; }
        public GlVersion Version { get; }
        public GlInterface GlInterface { get; }
        public int SampleCount { get; }
        public int StencilSize { get; }
        public EglDisplay Display => _disp;
        public EglInterface EglInterface => _egl;

        private class RestoreContext : IDisposable
        {
            private readonly EglInterface _egl;
            private readonly object _l;
            private readonly IntPtr _display;
            private readonly IntPtr _context, _read, _draw;

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

        public IDisposable MakeCurrent(EglSurface? surface)
        {
            if (IsLost)
                throw new PlatformGraphicsContextLostException();
            
            Monitor.Enter(_lock);
            var success = false;
            try
            {
                var old = new RestoreContext(_egl, _disp.Handle, _lock);
                var surf = surface ?? OffscreenSurface;
                _egl.MakeCurrent(_disp.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (!_egl.MakeCurrent(_disp.Handle, surf?.DangerousGetHandle() ?? IntPtr.Zero,
                        surf?.DangerousGetHandle() ?? IntPtr.Zero, Context))
                {
                    var error = _egl.GetError();
                    if (error == EGL_CONTEXT_LOST)
                    {
                        NotifyContextLost();
                        throw new PlatformGraphicsContextLostException();
                    }

                    throw OpenGlException.GetFormattedEglException("eglMakeCurrent", error);
                }

                success = true;
                return old;
            }
            finally
            {
                if(!success)
                    Monitor.Exit(_lock);
            }
        }

        public void NotifyContextLost()
        {
            _isLost = true;
            _disp.OnContextLost(this);
        }
        
        public bool IsLost => _isLost || _disp.IsLost || Context == IntPtr.Zero;

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

        public bool CanCreateSharedContext => _disp.SupportsSharing;

        public IGlContext CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null) =>
            _disp.CreateContext(new EglContextOptions
            {
                ShareWith = _sharedWith ?? this
            });

        public bool IsCurrent => _context != default && _egl.GetCurrentDisplay() == _disp.Handle && _egl.GetCurrentContext() == _context;

        public void Dispose()
        {
            if(_context == IntPtr.Zero)
                return;
            
            foreach(var f in _features.ToList())
                if (f.Value is IDisposable d)
                {
                    d.Dispose();
                    _features.Remove(f.Key);
                }

            _egl.DestroyContext(_disp.Handle, Context);
            OffscreenSurface?.Dispose();
            _context = IntPtr.Zero;
            _disp.OnContextDisposed(this);
            _disposeCallback?.Invoke();
        }

        public object? TryGetFeature(Type featureType)
        {
            if (_features.TryGetValue(featureType, out var feature))
                return feature;
            return null;
        }
    }
}
