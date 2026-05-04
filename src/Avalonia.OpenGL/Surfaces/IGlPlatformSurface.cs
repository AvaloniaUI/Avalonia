using Avalonia.Platform.Surfaces;

namespace Avalonia.OpenGL.Surfaces
{
    public interface IGlPlatformSurface : IPlatformRenderSurface
    {
        IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context);
    }
}
