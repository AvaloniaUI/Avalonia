using System;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;

namespace Avalonia.OpenGL.Surfaces
{
    [PrivateApi]
    public interface IGlPlatformSurfaceRenderTarget : IDisposable, IPlatformRenderSurfaceRenderTarget
    {
        IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo);
    }
}
