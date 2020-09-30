using Avalonia.OpenGL.Surfaces;

namespace Avalonia.OpenGL.Egl
{
    public class EglGlPlatformSurface : EglGlPlatformSurfaceBase
    {
        private readonly EglPlatformOpenGlInterface _egl;
        private readonly IEglWindowGlPlatformSurfaceInfo _info;
        
        public EglGlPlatformSurface(EglPlatformOpenGlInterface egl, IEglWindowGlPlatformSurfaceInfo info) : base()
        {
            _egl = egl;
            _info = info;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            var glSurface = _egl.CreateWindowSurface(_info.Handle);
            return new RenderTarget(_egl, glSurface, _info);
        }

        class RenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            private readonly EglPlatformOpenGlInterface _egl;
            private EglSurface _glSurface;
            private readonly IEglWindowGlPlatformSurfaceInfo _info;
            private PixelSize _currentSize;

            public RenderTarget(EglPlatformOpenGlInterface egl,
                EglSurface glSurface, IEglWindowGlPlatformSurfaceInfo info) : base(egl)
            {
                _egl = egl;
                _glSurface = glSurface;
                _info = info;
                _currentSize = info.Size;
            }

            public override void Dispose() => _glSurface.Dispose();
            
            public override IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                if (_info.Size != _currentSize || _glSurface == null)
                {
                    _glSurface?.Dispose();
                    _glSurface = null;
                    _glSurface = _egl.CreateWindowSurface(_info.Handle);
                    _currentSize = _info.Size;
                }
                return base.BeginDraw(_glSurface, _info);
            }
        }
    }
}

