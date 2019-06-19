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
        
        public EglGlPlatformSurface(EglDisplay display, EglContext context, IEglWindowGlPlatformSurfaceInfo info)
        {
            _display = display;
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
                    _context.MakeCurrent(_glSurface);
                    _display.EglInterface.WaitClient();
                    _display.EglInterface.WaitGL();
                    _display.EglInterface.WaitNative();
                    
                    return new Session(_display, _context, _glSurface, _info, l);
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
                

                public Session(EglDisplay display, EglContext context,
                    EglSurface glSurface, IEglWindowGlPlatformSurfaceInfo info,
                    IDisposable @lock)
                {
                    _context = context;
                    _display = display;
                    _glSurface = glSurface;
                    _info = info;
                    _lock = @lock;
                }

                public void Dispose()
                {
                    _context.Display.GlInterface.Flush();
                    _display.EglInterface.WaitGL();
                    _glSurface.SwapBuffers();
                    _display.EglInterface.WaitClient();
                    _display.EglInterface.WaitGL();
                    _display.EglInterface.WaitNative();
                    _context.Display.ClearContext();
                    _lock.Dispose();
                }

                public IGlDisplay Display => _context.Display;
                public PixelSize Size => _info.Size;
                public double Scaling => _info.Scaling;
            }
        }
    }
}

