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

/// <summary>
/// A rendering session backed by a <c>CAMetalDrawable</c>, for renderers that present the drawable themselves.
/// </summary>
[PrivateApi]
public interface IMetalPlatformSurfaceRenderingSessionWithDrawable : IMetalPlatformSurfaceRenderingSession
{
    /// <summary>The <c>id&lt;CAMetalDrawable&gt;</c> backing this session.</summary>
    IntPtr Drawable { get; }

    /// <summary>
    /// Stops the session from presenting <see cref="Drawable"/> on dispose. The caller must present it instead.
    /// </summary>
    void SuppressPresentation();
}
