using System;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.OpenGL.Surfaces
{
    public interface IGlPlatformSurfaceRenderingSession : IDisposable
    {
        IGlContext Context { get; }
        PixelSize Size { get; }
        double Scaling { get; }
        bool IsYFlipped { get; }
    }

    [PrivateApi]
    public interface IGlPlatformSurfaceRenderingSessionWithPartialPresentation : IGlPlatformSurfaceRenderingSession
    {
        IPartialPresentationFeature? PartialPresentation { get; }
    }
}
