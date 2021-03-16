using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.Android.OpenGL
{
    internal sealed class GlRenderTarget : EglPlatformSurfaceRenderTargetBase
    {
        private readonly EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo _info;
        private readonly EglSurface _surface;

        public GlRenderTarget(
            EglPlatformOpenGlInterface egl,
            EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo info,
            EglSurface surface)
            : base(egl)
        {
            _info = info;
            _surface = surface;
        }

        public override IGlPlatformSurfaceRenderingSession BeginDraw() => BeginDraw(_surface, _info);
    }
}
