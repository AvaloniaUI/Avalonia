using System;

namespace Avalonia.OpenGL
{
    public interface IEglWindowGlPlatformSurfaceInfo
    {
        IntPtr Handle { get; }
        int PixelWidth { get; }
        int PixelHeight { get; }
        Vector Dpi { get; }
        
    }
    
    public class EglGlPlatformSurface : IGlPlatformSurface
    {
        private readonly EglDisplay _display;
        private readonly IGlContext _context;
        private readonly IEglWindowGlPlatformSurfaceInfo _info;

        
        public EglGlPlatformSurface(EglDisplay display, IGlContext context, IEglWindowGlPlatformSurfaceInfo info)
        {
            _display = display;
            _context = context;
            _info = info;
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            var glSurface = _display.CreateWindowSurface(_info.Handle);
            return new RenderTarget(_context, glSurface, _info);
        }

        class RenderTarget : IGlPlatformSurfaceRenderTarget
        {
            private readonly IGlContext _context;
            private readonly IGlSurface _glSurface;
            private readonly IEglWindowGlPlatformSurfaceInfo _info;

            public RenderTarget(IGlContext context, IGlSurface glSurface, IEglWindowGlPlatformSurfaceInfo info)
            {
                _context = context;
                _glSurface = glSurface;
                _info = info;
            }

            public void Dispose() => _glSurface.Dispose();

            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                _context.MakeCurrent(_glSurface);
                return new Session(_context, _glSurface, _info);
            }
            
            class Session : IGlPlatformSurfaceRenderingSession
            {
                private readonly IGlContext _context;
                private readonly IGlSurface _glSurface;
                private readonly IEglWindowGlPlatformSurfaceInfo _info;

                public Session(IGlContext context, IGlSurface glSurface, IEglWindowGlPlatformSurfaceInfo info)
                {
                    _context = context;
                    _glSurface = glSurface;
                    _info = info;
                }

                public void Dispose()
                {
                    _context.Display.GlInterface.Flush();
                    _glSurface.SwapBuffers();
                    _context.Display.ClearContext();
                }

                public IGlDisplay Display => _context.Display;
                public int PixelWidth => _info.PixelWidth;
                public int PixelHeight => _info.PixelHeight;
                public Vector Dpi => _info.Dpi;
            }
        }
    }
}
