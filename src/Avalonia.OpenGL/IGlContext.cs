using System;
using System.Collections.Generic;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;

namespace Avalonia.OpenGL
{
    public interface IGlContext : IPlatformGraphicsContext
    {
        GlVersion Version { get; }
        GlInterface GlInterface { get; }
        int SampleCount { get; }
        int StencilSize { get; }
        IDisposable MakeCurrent();
        bool IsSharedWith(IGlContext context);
        bool CanCreateSharedContext { get; }
        IGlContext? CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null);
    }

    public interface IGlPlatformSurfaceRenderTargetFactory
    {
        bool CanRenderToSurface(IGlContext context, IPlatformRenderSurface surface);
        IGlPlatformSurfaceRenderTarget CreateRenderTarget(IGlContext context, IPlatformRenderSurface surface);
    }
}
