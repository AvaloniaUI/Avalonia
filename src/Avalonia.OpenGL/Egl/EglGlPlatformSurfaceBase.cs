using System;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.OpenGL.Egl
{
    public abstract class EglGlPlatformSurfaceBase : IGlPlatformSurface
    {
        public interface IEglWindowGlPlatformSurfaceInfo
        {
            IntPtr Handle { get; }
            PixelSize Size { get; }
            double Scaling { get; }
        }

        public abstract IGlPlatformSurfaceRenderTarget CreateGlRenderTarget();
    }

    public abstract class EglPlatformSurfaceRenderTargetBase  : IGlPlatformSurfaceRenderTarget
    {
        private readonly EglPlatformOpenGlInterface _egl;

        protected EglPlatformSurfaceRenderTargetBase(EglPlatformOpenGlInterface egl)
        {
            _egl = egl;
        }

        public virtual void Dispose()
        {
            
        }

        public abstract IGlPlatformSurfaceRenderingSession BeginDraw();

        protected IGlPlatformSurfaceRenderingSession BeginDraw(EglSurface surface,
            EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo info, Action onFinish = null, bool isYFlipped = false)
        {

            var restoreContext = _egl.PrimaryEglContext.MakeCurrent(surface);
            var success = false;
            try
            {
                var egli = _egl.Display.EglInterface;
                egli.WaitClient();
                egli.WaitGL();
                egli.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
                
                _egl.PrimaryContext.GlInterface.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, 0);
                
                success = true;
                return new Session(_egl.Display, _egl.PrimaryEglContext, surface, info,  restoreContext, onFinish, isYFlipped);
            }
            finally
            {
                if(!success)
                    restoreContext.Dispose();
            }
        }
        
        class Session : IGlPlatformSurfaceRenderingSession
        {
            private readonly EglContext _context;
            private readonly EglSurface _glSurface;
            private readonly EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo _info;
            private readonly EglDisplay _display;
            private readonly IDisposable _restoreContext;
            private readonly Action _onFinish;


            public Session(EglDisplay display, EglContext context,
                EglSurface glSurface, EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo info,
                 IDisposable restoreContext, Action onFinish, bool isYFlipped)
            {
                IsYFlipped = isYFlipped;
                _context = context;
                _display = display;
                _glSurface = glSurface;
                _info = info;
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
            public PixelSize Size => _info.Size;
            public double Scaling => _info.Scaling;
            public bool IsYFlipped { get; }
        }
    }
}
