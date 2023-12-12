using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.Tizen;
internal class NuiGlLayerSurface : IGlPlatformSurface
{
    private readonly NuiAvaloniaView _nuiAvaloniaView;

    public NuiGlLayerSurface(NuiAvaloniaView nuiAvaloniaView)
    {
        _nuiAvaloniaView = nuiAvaloniaView;
    }

    public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
    {
        var ctx = TizenPlatform.GlPlatform.Context;
        if (ctx != context)
            throw new InvalidOperationException("Platform surface is only usable with tha main context");
        using (ctx.MakeCurrent())
        {
            return new RenderTarget(ctx, _nuiAvaloniaView);
        }
    }

    class RenderTarget : IGlPlatformSurfaceRenderTarget
    {
        private readonly GlContext _ctx;
        private readonly NuiAvaloniaView _nuiAvaloniaView;

        public RenderTarget(GlContext ctx, NuiAvaloniaView nuiAvaloniaView)
        {
            _ctx = ctx;
            _nuiAvaloniaView = nuiAvaloniaView;
        }

        public void Dispose()
        {
            
        }

        public IGlPlatformSurfaceRenderingSession BeginDraw()
        {
            var restoreContext = _ctx.MakeCurrent();
            return new RenderSession(_ctx, restoreContext, _nuiAvaloniaView);
        }
    }

    class RenderSession : IGlPlatformSurfaceRenderingSession
    {
        private readonly GlContext _ctx;
        private readonly IDisposable _restoreContext;

        public RenderSession(GlContext ctx, IDisposable restoreContext, NuiAvaloniaView nuiAvaloniaView)
        {
            _ctx = ctx;
            _restoreContext = restoreContext;
            Size = new PixelSize((int)nuiAvaloniaView.Size.Width, (int)nuiAvaloniaView.Size.Height);
            Scaling = nuiAvaloniaView.Scaling;
            Context = ctx;
        }

        public void Dispose()
        {
            _ctx.GlInterface.Finish();
            _restoreContext.Dispose();
        }

        public IGlContext Context { get; }
        public PixelSize Size { get; }
        public double Scaling { get; }
        public bool IsYFlipped { get; }
    }
}
