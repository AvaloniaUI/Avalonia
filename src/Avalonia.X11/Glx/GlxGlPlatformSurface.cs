using System;
using Avalonia.OpenGL;

namespace Avalonia.X11.Glx
{
    class GlxGlPlatformSurface: IGlPlatformSurface
    {

        private readonly GlxDisplay _display;
        private readonly GlxContext _context;
        private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
        
        public GlxGlPlatformSurface(GlxDisplay display, GlxContext context, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
        {
            _display = display;
            _context = context;
            _info = info;
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            return new RenderTarget(_context, _info);
        }

        class RenderTarget : IGlPlatformSurfaceRenderTarget
        {
            private readonly GlxContext _context;
            private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;

            public RenderTarget(GlxContext context,  EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
            {
                _context = context;
                _info = info;
            }

            public void Dispose()
            {
                // No-op
            }

            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var l = _context.Lock();
                try
                {
                    _context.MakeCurrent(_info.Handle);
                    return new Session(_context, _info, l);
                }
                catch
                {
                    l.Dispose();
                    throw;
                }
            }
            
            class Session : IGlPlatformSurfaceRenderingSession
            {
                private readonly GlxContext _context;
                private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
                private IDisposable _lock;

                public Session(GlxContext context, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info,
                    IDisposable @lock)
                {
                    _context = context;
                    _info = info;
                    _lock = @lock;
                }

                public void Dispose()
                {
                    _context.Display.GlInterface.Flush();
                    _context.Glx.WaitGL();
                    _context.Display.SwapBuffers(_info.Handle);
                    _context.Glx.WaitX();
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
