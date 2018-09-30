using System;
using System.Reactive.Disposables;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;
namespace Avalonia.Skia
{
    public class GlRenderTarget : IRenderTarget
    {
        private readonly GRContext _grContext;
        private IGlPlatformSurfaceRenderTarget _surface;

        public GlRenderTarget(GRContext grContext, IGlPlatformSurface glSurface)
        {
            _grContext = grContext;
            _surface = glSurface.CreateGlRenderTarget();
        }

        public void Dispose() => _surface.Dispose();

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var session = _surface.BeginDraw();
            var disp = session.Display;
            var gl = disp.GlInterface;
            gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var fb);
            GRBackendRenderTargetDesc desc = new GRBackendRenderTargetDesc
            {
                Width = session.PixelWidth,
                Height = session.PixelHeight,
                SampleCount = disp.SampleCount,
                StencilBits = disp.StencilSize,
                Config = GRPixelConfig.Rgba8888,
                Origin=GRSurfaceOrigin.BottomLeft,
                RenderTargetHandle = new IntPtr(fb)
            };
            gl.Viewport(0, 0, desc.Width, desc.Height);
            gl.ClearStencil(0);
            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
            var surface = SKSurface.Create(_grContext, desc);

            var nfo = new DrawingContextImpl.CreateInfo
            {
                GrContext = _grContext,
                Canvas = surface.Canvas,
                Dpi = session.Dpi,
                VisualBrushRenderer = visualBrushRenderer,
                DisableTextLcdRendering = true
            };
            return new DrawingContextImpl(nfo, Disposable.Create(() =>
            {
                surface.Canvas.Flush();
                surface.Dispose();
                session.Dispose();
            }));

        }
    }
}
