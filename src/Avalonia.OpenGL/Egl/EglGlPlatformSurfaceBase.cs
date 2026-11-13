using System;
using Avalonia.Metadata;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;

namespace Avalonia.OpenGL.Egl
{
    public abstract class EglGlPlatformSurfaceBase : IGlPlatformSurface
    {
        public abstract IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context);
    }

    public abstract class EglPlatformSurfaceRenderTargetBase : IGlPlatformSurfaceRenderTarget
    {
        protected EglContext Context { get; }
        private EglPartialPresentation? _partialPresentation;
        private EglSurface? _partialPresentationSurface;
        private bool _partialPresentationCorrupted;

        [PrivateApi]
        protected virtual bool UsePartialPresentation => false;

        [PrivateApi]
        protected virtual void OnSwapBuffersFailed() { }

        RenderTargetProperties IGlPlatformSurfaceRenderTarget.Properties => Properties;

        [PrivateApi]
        protected virtual RenderTargetProperties Properties => new()
        {
            SupportsPartialPresentation = UsePartialPresentation && Context.Display.SupportsBufferAge,
            IsSuitableForDirectRendering = UsePartialPresentation && Context.StencilSize >= 8
                && _partialPresentation?.PreferRetainedLayer != true
        };

        protected EglPlatformSurfaceRenderTargetBase(EglContext context)
        {
            Context = context;
        }

        public virtual void Dispose()
        {
            _partialPresentation?.Invalidate();
            _partialPresentationSurface = null;
        }

        public IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            if (IsCorrupted)
                throw new RenderTargetCorruptedException();
            
            return BeginDrawCore(sceneInfo);
        }

        protected virtual bool SkipWaits => false;

        public abstract IGlPlatformSurfaceRenderingSession BeginDrawCore(IRenderTarget.RenderTargetSceneInfo sceneInfo);

        protected IGlPlatformSurfaceRenderingSession BeginDraw(EglSurface surface,
            PixelSize size, double scaling, Action? onFinish = null, bool isYFlipped = false,
            Action? beforeSwap = null)
        {

            var restoreContext = Context.MakeCurrent(surface);
            var success = false;
            try
            {
                var egli = Context.Display.EglInterface;
                if (!SkipWaits)
                {
                    egli.WaitClient();
                    egli.WaitGL();
                    egli.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
                }

                Context.GlInterface.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, 0);
                
                // Workaround for driver quirk https://github.com/NVIDIA/egl-wayland2/issues/46
                // This is NVIDIA-specific, but setting buffers to GL_BACK won't hurt for other drivers too
                if (Context.Version.Type == GlProfileType.OpenGL)
                {
                    var gl = Context.GlInterface;
                    gl.Viewport(0, 0, size.Width, size.Height);
                    if (gl.IsReadBufferAvailable)
                        gl.ReadBuffer(GlConsts.GL_BACK);
                    if (gl.IsWriteBufferAvailable)
                        gl.WriteBuffer(GlConsts.GL_BACK);
                    if(gl.IsDrawBufferAvailable)
                        gl.DrawBuffer(GlConsts.GL_BACK);
                }

                
                EglPartialPresentation.Session? partial = null;
                if (Properties.SupportsPartialPresentation)
                {
                    var display = Context.Display;
                    _partialPresentation ??= new EglPartialPresentation(egli, display.Handle,
                        display.SupportsPartialUpdate, display.SupportsSwapBuffersWithDamageKhr,
                        display.SupportsSwapBuffersWithDamageExt, () => _partialPresentationCorrupted = true);
                    // EGL can reuse a destroyed surface's native handle value.
                    if (!ReferenceEquals(_partialPresentationSurface, surface))
                    {
                        _partialPresentation.Invalidate();
                        _partialPresentationSurface = surface;
                    }
                    partial = _partialPresentation.BeginDraw(surface.DangerousGetHandle(), size, isYFlipped);
                }
                success = true;
                return new Session(Context.Display, Context, surface, size, scaling, restoreContext,
                    onFinish, isYFlipped, SkipWaits, beforeSwap, partial, OnSwapBuffersFailed);
            }
            finally
            {
                if(!success)
                {
                    if (Properties.SupportsPartialPresentation)
                        _partialPresentation?.Invalidate();
                    restoreContext.Dispose();
                }
            }
        }

        private class Session : IGlPlatformSurfaceRenderingSessionWithPartialPresentation
        {
            private readonly EglContext _context;
            private readonly EglSurface _glSurface;
            private readonly EglDisplay _display;
            private readonly IDisposable _restoreContext;
            private readonly Action? _onFinish;
            private readonly Action? _beforeSwap;
            private readonly bool _skipWaits;
            private readonly EglPartialPresentation.Session? _partial;
            private readonly Action _onSwapFailed;
            private bool _disposed;

            public Session(EglDisplay display, EglContext context,
                EglSurface glSurface, PixelSize size, double scaling,
                IDisposable restoreContext, Action? onFinish, bool isYFlipped, bool skipWaits,
                Action? beforeSwap, EglPartialPresentation.Session? partial, Action onSwapFailed)
            {
                Size = size;
                Scaling = scaling;
                IsYFlipped = isYFlipped;
                _context = context;
                _display = display;
                _glSurface = glSurface;
                _restoreContext = restoreContext;
                _onFinish = onFinish;
                _beforeSwap = beforeSwap;
                _skipWaits = skipWaits;
                _partial = partial;
                _onSwapFailed = onSwapFailed;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                var success = false;
                try
                {
                    if (_partial is { CanPresent: false })
                        return;
                    _context.GlInterface.Flush();
                    if (!_skipWaits)
                        _display.EglInterface.WaitGL();
                    _beforeSwap?.Invoke();
                    var presented = false;
                    try
                    {
                        if (_partial != null)
                            presented = _partial.Present();
                        else
                        {
                            _glSurface.SwapBuffers();
                            presented = true;
                        }
                    }
                    finally
                    {
                        if (!presented)
                            _onSwapFailed();
                    }
                    if (!presented)
                        return;
                    if (!_skipWaits)
                    {
                        _display.EglInterface.WaitClient();
                        _display.EglInterface.WaitGL();
                        _display.EglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
                    }
                    success = true;
                }
                finally
                {
                    _partial?.Dispose();
                    _restoreContext.Dispose();
                    if (success)
                        _onFinish?.Invoke();
                }
            }

            public IPartialPresentationFeature? PartialPresentation => _partial;
            public IGlContext Context => _context;
            public PixelSize Size { get; }
            public double Scaling { get; }
            public bool IsYFlipped { get; }
        }

        public virtual PlatformRenderTargetState State =>
            IsCorrupted ? PlatformRenderTargetState.Corrupted : PlatformRenderTargetState.Ready;
        
        public virtual bool IsCorrupted => Context.IsLost || _partialPresentationCorrupted;
    }
}
