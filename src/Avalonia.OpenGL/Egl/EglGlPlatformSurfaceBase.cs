using System;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.OpenGL.Egl
{
    public abstract class EglGlPlatformSurfaceBase : IGlPlatformSurface
    {
        public abstract IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context);
    }

    public abstract class EglPlatformSurfaceRenderTargetBase : IGlPlatformSurfaceRenderTargetWithCorruptionInfo
    {
        protected EglContext Context { get; }

        protected EglPlatformSurfaceRenderTargetBase(EglContext context)
        {
            Context = context;
        }

        public virtual void Dispose()
        {
            
        }

        public IGlPlatformSurfaceRenderingSession BeginDraw()
        {
            if (Context.IsLost)
                throw new RenderTargetCorruptedException();
            
            return BeginDrawCore();
        }

        public abstract IGlPlatformSurfaceRenderingSession BeginDrawCore();

        protected IGlPlatformSurfaceRenderingSession BeginDraw(EglSurface surface,
            PixelSize size, double scaling, Action? onFinish = null, bool isYFlipped = false)
        {

            var restoreContext = Context.MakeCurrent(surface);
            var success = false;
            try
            {
                var egli = Context.Display.EglInterface;
                egli.WaitClient();
                egli.WaitGL();
                egli.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
                
                Context.GlInterface.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, 0);
                
                success = true;
                return new Session(Context.Display, Context, surface, size, scaling,  restoreContext, onFinish, isYFlipped);
            }
            finally
            {
                if(!success)
                    restoreContext.Dispose();
            }
        }

        private class Session : IGlPlatformSurfaceRenderingSession
        {
            private readonly EglContext _context;
            private readonly EglSurface _glSurface;
            private readonly EglDisplay _display;
            private readonly IDisposable _restoreContext;
            private readonly Action? _onFinish;

            public Session(EglDisplay display, EglContext context,
                EglSurface glSurface, PixelSize size, double scaling,
                IDisposable restoreContext, Action? onFinish, bool isYFlipped)
            {
                Size = size;
                Scaling = scaling;
                IsYFlipped = isYFlipped;
                _context = context;
                _display = display;
                _glSurface = glSurface;
                _restoreContext = restoreContext;
                _onFinish = onFinish;
            }

            public void Dispose()
            {
                _context.GlInterface.Flush();
                _display.EglInterface.WaitGL();
                _glSurface.SwapBuffers();
                _display.EglInterface.WaitClient();
                _display.EglInterface.WaitGL();
                _display.EglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
                _restoreContext.Dispose();
                _onFinish?.Invoke();
            }

            public IGlContext Context => _context;
            public PixelSize Size { get; }
            public double Scaling { get; }
            public bool IsYFlipped { get; }
        }

        public virtual bool IsCorrupted => Context.IsLost;
    }
}
