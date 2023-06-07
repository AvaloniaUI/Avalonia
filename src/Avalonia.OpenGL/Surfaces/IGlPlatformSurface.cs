namespace Avalonia.OpenGL.Surfaces
{
    public interface IGlPlatformSurface
    {
        IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context);
    }
}
