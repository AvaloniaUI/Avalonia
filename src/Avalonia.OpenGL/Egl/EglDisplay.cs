using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Reactive;
using static Avalonia.OpenGL.Egl.EglConsts;

namespace Avalonia.OpenGL.Egl
{
    public class EglDisplay : IDisposable
    {
        private readonly EglInterface _egl;
        private IntPtr _display;
        private readonly EglDisplayOptions _options;
        private EglConfigInfo _config;
        private bool _isLost;
        private readonly object _lock = new();
        
        public bool SupportsSharing { get; }

        public IntPtr Handle => _display;
        public IntPtr Config => _config.Config;
        internal bool SingleContext => !_options.SupportsMultipleContexts;
        private readonly List<EglContext> _contexts = new();
        
        public EglDisplay() : this(new EglDisplayCreationOptions
        {
            Egl = new EglInterface()
        })
        {

        }

        public EglDisplay(EglDisplayCreationOptions options) : this(EglDisplayUtils.CreateDisplay(options), options)
        {
            
        }
        
        public EglDisplay(IntPtr display, EglDisplayOptions options)
        {
            _egl = options.Egl ?? new EglInterface();
            SupportsSharing = options.SupportsContextSharing;
            _display = display;
            _options = options;
            if(_display == IntPtr.Zero)
                throw new ArgumentException();

            _config = EglDisplayUtils.InitializeAndGetConfig(_egl, display, options.GlVersions);
        }
        
        public EglInterface EglInterface => _egl;
        public EglContext CreateContext(EglContextOptions? options)
        {
            if (SingleContext && _contexts.Any())
                throw new OpenGlException("This EGLDisplay can only have one active context");
            
            options ??= new EglContextOptions();
            lock (_lock)
            {
                var share = options.ShareWith;
                if (share != null && !SupportsSharing)
                    throw new NotSupportedException("Context sharing is not supported by this display");

                var offscreenSurface = options.OffscreenSurface;

                if (offscreenSurface == null)
                {
                    // Check if eglMakeCurrent can work with EGL_NONE as read-write surfaces
                    var extensions = _egl.QueryString(Handle, EGL_EXTENSIONS);
                    if (extensions?.Contains("EGL_KHR_surfaceless_context") != true)
                    {
                        // Attempt to create a PBuffer as a surface for offscreen rendering
                        if ((_config.SurfaceType | EGL_PBUFFER_BIT) == 0)
                            throw new InvalidOperationException(
                                "Platform doesn't support EGL_KHR_surfaceless_context and PBUFFER surfaces");
                        
                        var pBufferSurface = _egl.CreatePBufferSurface(_display, Config,
                            new[] { EGL_WIDTH, 1, EGL_HEIGHT, 1, EGL_NONE });
                        if (pBufferSurface == IntPtr.Zero)
                            throw OpenGlException.GetFormattedException("eglCreatePBufferSurface", _egl);

                        offscreenSurface = new EglSurface(this, pBufferSurface);
                    }
                }

                var ctx = _egl.CreateContext(_display, Config, share?.Context ?? IntPtr.Zero, _config.Attributes);
                if (ctx == IntPtr.Zero)
                {
                    var ex = OpenGlException.GetFormattedException("eglCreateContext", _egl);
                    offscreenSurface?.Dispose();
                    throw ex;
                }

                var rv = new EglContext(this, _egl, share, ctx, offscreenSurface,
                    _config.Version, _config.SampleCount, _config.StencilSize,
                    options.DisposeCallback, options.ExtraFeatures ?? new());
                _contexts.Add(rv);
                return rv;
            }
        }

        public EglSurface CreateWindowSurface(IntPtr window)
        {
            if (window == IntPtr.Zero)
                throw new OpenGlException($"Window {window} is invalid.");

            using (Lock())
            {
                var s = EglInterface.CreateWindowSurface(Handle, Config, window,
                    new[] { EGL_NONE, EGL_NONE });
                if (s == IntPtr.Zero)
                    throw OpenGlException.GetFormattedException("eglCreateWindowSurface", EglInterface);
                return new EglSurface(this, s);
            }
        }

        public unsafe EglSurface CreatePBufferFromClientBuffer(int bufferType, IntPtr handle, int[] attribs)
        {
            fixed (int* attrs = attribs)
            {
                return CreatePBufferFromClientBuffer(bufferType, handle, attrs);
            }
        }
        
        public unsafe EglSurface CreatePBufferFromClientBuffer (int bufferType, IntPtr handle, int* attribs)
        {
            using (Lock())
            {
                var s = EglInterface.CreatePbufferFromClientBufferPtr(Handle, bufferType, handle,
                    Config, attribs);

                if (s == IntPtr.Zero)
                    throw OpenGlException.GetFormattedException("eglCreatePbufferFromClientBuffer", EglInterface);
                return new EglSurface(this, s);
            }
        }

        protected virtual bool DisplayLockIsSharedWithContexts => false;
        
        internal object? ContextSharedSyncRoot => DisplayLockIsSharedWithContexts ? _lock : null;

        internal void OnContextLost(EglContext context)
        {
            if (_options.ContextLossIsDisplayLoss)
                _isLost = true;
        }

        internal void OnContextDisposed(EglContext context)
        {
            lock (_lock)
                _contexts.Remove(context);
        }
        
        public bool IsLost
        {
            get
            {
                if (_isLost || _display == IntPtr.Zero)
                    return true;
                if (_options.DeviceLostCheckCallback?.Invoke() == true)
                    return _isLost = true;
                return false;
            }
        }
        
        public IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return Disposable.Create(() => { Monitor.Exit(_lock); });
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach(var ctx in _contexts)
                    ctx.Dispose();
                _contexts.Clear();
                if (_display != IntPtr.Zero)
                    _egl.Terminate(_display);
                _display = IntPtr.Zero;
                _options.DisposeCallback?.Invoke();
            }
        }
    }
}
