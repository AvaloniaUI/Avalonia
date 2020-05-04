using System;
using System.Threading;

namespace Avalonia.OpenGL
{
    public class EglGlPlatformSurface : IGlPlatformSurface
    {
        public interface IEglWindowGlPlatformSurfaceInfo
        {
            IntPtr Handle { get; }
            PixelSize Size { get; }
            double Scaling { get; }
        }

        private readonly EglDisplay _display;
        private readonly EglContext _context;
        private readonly IEglWindowGlPlatformSurfaceInfo _info;
        
        public EglGlPlatformSurface(EglContext context, IEglWindowGlPlatformSurfaceInfo info)
        {
            _display = context.Display;
            _context = context;
            _info = info;
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            var glSurface = _display.CreateWindowSurface(_info.Handle);
            return new RenderTarget(_display, _context, glSurface, _info);
        }

        class RenderTarget : IGlPlatformSurfaceRenderTargetWithCorruptionInfo
        {
            private readonly EglDisplay _display;
            private readonly EglContext _context;
            private readonly EglSurface _glSurface;
            private readonly IEglWindowGlPlatformSurfaceInfo _info;
            private PixelSize _initialSize;

            public RenderTarget(EglDisplay display, EglContext context,
                EglSurface glSurface, IEglWindowGlPlatformSurfaceInfo info)
            {
                _display = display;
                _context = context;
                _glSurface = glSurface;
                _info = info;
                _initialSize = info.Size;
            }

            public void Dispose() => _glSurface.Dispose();

            public bool IsCorrupted => _initialSize != _info.Size;
            
            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var l = _context.Lock();
                try
                {
                    if (IsCorrupted)
                        throw new RenderTargetCorruptedException();
                    var restoreContext = _context.MakeCurrent(_glSurface);
                    _display.EglInterface.WaitClient();
                    _display.EglInterface.WaitGL();
                    _display.EglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
                    
                    return new Session(_display, _context, _glSurface, _info, l, restoreContext);
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
                private readonly IEglWindowGlPlatformSurfaceInfo _info;
                private readonly EglDisplay _display;
                private IDisposable _lock;
                private readonly IDisposable _restoreContext;


                public Session(EglDisplay display, EglContext context,
                    EglSurface glSurface, IEglWindowGlPlatformSurfaceInfo info,
                    IDisposable @lock, IDisposable restoreContext)
                {
                    _context = context;
                    _display = display;
                    _glSurface = glSurface;
                    _info = info;
                    _lock = @lock;
                    _restoreContext = restoreContext;
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
                }

                public IGlContext Context => _context;
                public PixelSize Size => _info.Size;
                public double Scaling => _info.Scaling;
                public bool IsYFlipped { get; }
            }
        }
    }
}

