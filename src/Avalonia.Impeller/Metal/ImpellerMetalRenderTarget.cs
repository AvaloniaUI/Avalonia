using System;
using Avalonia.Metal;
using Avalonia.Platform;

namespace Avalonia.Impeller;

/// <summary>
/// Wraps the window's CAMetalDrawable as an Impeller surface each frame. Impeller submits on its own queue,
/// so it presents the drawable instead of the session.
/// </summary>
internal class ImpellerMetalRenderTarget : IRenderTarget
{
    private readonly ImpellerMetalBackendContext _context;
    private readonly IMetalPlatformSurfaceRenderTarget _target;

    public ImpellerMetalRenderTarget(ImpellerMetalBackendContext context, IMetalPlatformSurfaceRenderTarget target)
    {
        _context = context;
        _target = target;
    }

    public RenderTargetProperties Properties => new()
    {
        // Drawables don't keep their contents, so render directly and redraw everything each frame.
        RetainsPreviousFrameContents = true,
        IsSuitableForDirectRendering = true
    };

    public PlatformRenderTargetState PlatformRenderTargetState => _target.State;

    public IDrawingContextImpl CreateDrawingContext(IRenderTarget.RenderTargetSceneInfo sceneInfo,
        out RenderTargetDrawingContextProperties properties)
    {
        properties = new RenderTargetDrawingContextProperties { PreviousFrameIsRetained = false };

        var session = _target.BeginRendering();
        var success = false;
        try
        {
            if (session is not IMetalPlatformSurfaceRenderingSessionWithDrawable drawableSession)
                throw new NotSupportedException(
                    "The Impeller Metal backend requires a rendering session that exposes its CAMetalDrawable " +
                    $"(IMetalPlatformSurfaceRenderingSessionWithDrawable); got {session.GetType()}.");

            var size = session.Size;
            if (size.Width <= 0 || size.Height <= 0)
                throw new InvalidOperationException(
                    $"Can't create an Impeller drawing context for a {size} surface.");

            var impellerSurface =
                _context.ImpellerContext.SurfaceCreateWrappedMetalDrawableNew(drawableSession.Drawable)
                ?? throw new InvalidOperationException("ImpellerContext failed to wrap the Metal drawable.");

            // Otherwise the session still presents the drawable on dispose.
            drawableSession.SuppressPresentation();

            success = true;
            return new ImpellerDrawingContext(_context, impellerSurface, session, size,
                session.IsYFlipped, presentsSurface: true);
        }
        finally
        {
            if (!success)
                session.Dispose();
        }
    }

    public void Dispose() => _target.Dispose();
}
