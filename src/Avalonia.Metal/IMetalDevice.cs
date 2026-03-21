using System;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;

namespace Avalonia.Metal;


[PrivateApi]
public interface IMetalDevice : IPlatformGraphicsContext
{
    IntPtr Device { get; }
    IntPtr CommandQueue { get; }
}

[PrivateApi]
public interface IMetalPlatformSurface : IPlatformRenderSurface
{
    IMetalPlatformSurfaceRenderTarget CreateMetalRenderTarget(IMetalDevice device);
}

[PrivateApi]
public interface IMetalPlatformSurfaceRenderTarget : IDisposable, IPlatformRenderSurfaceRenderTarget
{
    IMetalPlatformSurfaceRenderingSession BeginRendering();
}

[PrivateApi]
public interface IMetalPlatformSurfaceRenderingSession : IDisposable
{
    IntPtr Texture { get; }
    PixelSize Size { get; }
    double Scaling { get; }
    bool IsYFlipped { get; }
}
