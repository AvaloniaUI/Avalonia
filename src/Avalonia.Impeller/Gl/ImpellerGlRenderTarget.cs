using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using NImpeller;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Impeller;

/// <summary>Wraps the window framebuffer as an Impeller surface each frame.</summary>
internal class ImpellerGlRenderTarget : IRenderTarget
{
    private readonly ImpellerGlBackendContext _context;
    private readonly IGlPlatformSurfaceRenderTarget _surface;

    public ImpellerGlRenderTarget(ImpellerGlBackendContext context, IGlPlatformSurface glSurface)
    {
        _context = context;
        using (context.GlContext.EnsureCurrent())
            _surface = glSurface.CreateGlRenderTarget(context.GlContext);
    }

    public RenderTargetProperties Properties => new()
    {
        // Impeller clears the surface it draws into, so render directly and redraw everything each frame.
        RetainsPreviousFrameContents = true,
        IsSuitableForDirectRendering = true
    };

    public PlatformRenderTargetState PlatformRenderTargetState => _surface.State;

    public IDrawingContextImpl CreateDrawingContext(IRenderTarget.RenderTargetSceneInfo sceneInfo,
        out RenderTargetDrawingContextProperties properties)
    {
        properties = new RenderTargetDrawingContextProperties { PreviousFrameIsRetained = false };

        var session = _surface.BeginDraw(sceneInfo);
        var success = false;
        try
        {
            var size = session.Size;
            if (size.Width <= 0 || size.Height <= 0)
            {
                session.Dispose();
                throw new System.InvalidOperationException(
                    $"Can't create an Impeller drawing context for a {size} surface.");
            }

            // Wayland DMA-BUF binds a framebuffer other than 0.
            session.Context.GlInterface.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var fbo);

            var impellerSurface = _context.ImpellerContext.SurfaceCreateWrappedFBONew(
                (ulong)fbo,
                ImpellerPixelFormat.kImpellerPixelFormatRGBA8888,
                new ImpellerISize(size.Width, size.Height))
                ?? throw new System.InvalidOperationException("ImpellerContext failed to wrap the window FBO.");

            success = true;
            return new ImpellerDrawingContext(_context, impellerSurface, session, size, session.IsYFlipped);
        }
        finally
        {
            if (!success)
                session.Dispose();
        }
    }

    public void Dispose() => _surface.Dispose();
}
