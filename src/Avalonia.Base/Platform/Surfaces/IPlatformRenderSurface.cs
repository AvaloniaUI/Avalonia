using Avalonia.Metadata;

namespace Avalonia.Platform.Surfaces;

[PrivateApi]
public interface IPlatformRenderSurface
{
    bool IsReady => true;
}

[PrivateApi]
public interface IPlatformRenderSurfaceRenderTarget
{
    bool IsReady => true;
}
