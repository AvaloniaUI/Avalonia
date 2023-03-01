using System;
using System.Collections.Generic;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;

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
        bool CanRenderToSurface(IGlContext context, object surface);
        IGlPlatformSurfaceRenderTarget CreateRenderTarget(IGlContext context, object surface);
    }
}
