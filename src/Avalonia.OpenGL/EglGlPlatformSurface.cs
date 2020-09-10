using System;
using System.Threading;

namespace Avalonia.OpenGL
{
    public class EglGlPlatformSurface : EglGlPlatformSurfaceBase
    {
        private readonly EglDisplay _display;
        private readonly EglContext _context;
        private readonly IEglWindowGlPlatformSurfaceInfo _info;
        
        public EglGlPlatformSurface(EglContext context, IEglWindowGlPlatformSurfaceInfo info) : base()
        {
            _display = context.Display;
            _context = context;
            _info = info;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            var glSurface = _display.CreateWindowSurface(_info.Handle);
            return new RenderTarget(_display, _context, glSurface, _info);
        }

        class RenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            private readonly EglDisplay _display;
            private readonly EglContext _context;
            private readonly EglSurface _glSurface;
            private readonly IEglWindowGlPlatformSurfaceInfo _info;
            private PixelSize _initialSize;

            public RenderTarget(EglDisplay display, EglContext context,
                EglSurface glSurface, IEglWindowGlPlatformSurfaceInfo info) : base(display, context)
            {
                _display = display;
                _context = context;
                _glSurface = glSurface;
                _info = info;
                _initialSize = info.Size;
            }

            public override void Dispose() => _glSurface.Dispose();

            public override bool IsCorrupted => _initialSize != _info.Size;
            
            public override IGlPlatformSurfaceRenderingSession BeginDraw() => base.BeginDraw(_glSurface, _info);
        }
    }
}

