using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.Android.OpenGL
{
    internal sealed class GlPlatformSurface : EglGlPlatformSurfaceBase
    {
        private readonly EglPlatformOpenGlInterface _egl;
        private readonly IEglWindowGlPlatformSurfaceInfo _info;

        private GlPlatformSurface(EglPlatformOpenGlInterface egl, IEglWindowGlPlatformSurfaceInfo info)
        {
            _egl = egl;
            _info = info;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget() =>
            new GlRenderTarget(_egl, _info, _egl.CreateWindowSurface(_info.Handle), _info.Handle);

        public static GlPlatformSurface TryCreate(IEglWindowGlPlatformSurfaceInfo info)
        {
            if (EglPlatformOpenGlInterface.TryCreate() is EglPlatformOpenGlInterface egl)
            {
                return new GlPlatformSurface(egl, info);
            }

            return null;
        }
    }
}
