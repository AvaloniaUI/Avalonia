using System;

namespace Avalonia.OpenGL
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

    public abstract class EglPlatformSurfaceRenderTargetBase  : IGlPlatformSurfaceRenderTargetWithCorruptionInfo
    {
        private readonly EglDisplay _display;
        private readonly EglContext _context;

        protected EglPlatformSurfaceRenderTargetBase(EglDisplay display, EglContext context)
        {
            _display = display;
            _context = context;
        }

        public abstract bool IsCorrupted { get; }

        public virtual void Dispose()
        {
            
        }

        public abstract IGlPlatformSurfaceRenderingSession BeginDraw();

        protected IGlPlatformSurfaceRenderingSession BeginDraw(EglSurface surface,
            EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo info, Action onFinish = null, bool isYFlipped = false)
        {
            var l = _context.Lock();
            try
            {
                if (IsCorrupted)
                    throw new RenderTargetCorruptedException();
                var restoreContext = _context.MakeCurrent(surface);
                _display.EglInterface.WaitClient();
                _display.EglInterface.WaitGL();
                _display.EglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
                    
                return new Session(_display, _context, surface, info, l, restoreContext, onFinish, isYFlipped);
            }
            catch
            {
                l.Dispose();
                throw;
            }
        }
        
        class Session : IGlPlatformSurfaceRenderingSession
        {
            private readonly EglContext _context;
            private readonly EglSurface _glSurface;
            private readonly EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo _info;
            private readonly EglDisplay _display;
            private readonly IDisposable _lock;
            private readonly IDisposable _restoreContext;
            private readonly Action _onFinish;


            public Session(EglDisplay display, EglContext context,
                EglSurface glSurface, EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo info,
                IDisposable @lock, IDisposable restoreContext, Action onFinish, bool isYFlipped)
            {
                IsYFlipped = isYFlipped;
                _context = context;
                _display = display;
                _glSurface = glSurface;
                _info = info;
                _lock = @lock;
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
                _lock.Dispose();
                _onFinish?.Invoke();
            }

            public IGlContext Context => _context;
            public PixelSize Size => _info.Size;
            public double Scaling => _info.Scaling;
            public bool IsYFlipped { get; }
        }
    }
}
