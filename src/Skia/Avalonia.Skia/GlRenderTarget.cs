using System;
using System.Reactive.Disposables;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia
{
    internal class GlRenderTarget : IRenderTargetWithCorruptionInfo
    {
        private readonly GRContext _grContext;
        private IGlPlatformSurfaceRenderTarget _surface;

        public GlRenderTarget(GRContext grContext, IGlPlatformSurface glSurface)
        {
            _grContext = grContext;
            _surface = glSurface.CreateGlRenderTarget();
        }

        public void Dispose() => _surface.Dispose();

        public bool IsCorrupted => (_surface as IGlPlatformSurfaceRenderTargetWithCorruptionInfo)?.IsCorrupted == true;
        
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var session = _surface.BeginDraw();
            var disp = session.Display;
            var gl = disp.GlInterface;
            gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var fb);

            var size = session.Size;
            var scaling = session.Scaling;
            if (size.Width <= 0 || size.Height <= 0 || scaling < 0)
            {
                throw new InvalidOperationException(
                    $"Can't create drawing context for surface with {size} size and {scaling} scaling");
            }

            gl.Viewport(0, 0, size.Width, size.Height);
            gl.ClearStencil(0);
            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
            lock (_grContext)
            {
                _grContext.ResetContext();

                GRBackendRenderTarget renderTarget =
                    new GRBackendRenderTarget(size.Width, size.Height, disp.SampleCount, disp.StencilSize,
                        new GRGlFramebufferInfo((uint)fb, GRPixelConfig.Rgba8888.ToGlSizedFormat()));
                var surface = SKSurface.Create(_grContext, renderTarget,
                    GRSurfaceOrigin.BottomLeft,
                    GRPixelConfig.Rgba8888.ToColorType());

                var nfo = new DrawingContextImpl.CreateInfo
                {
                    GrContext = _grContext,
                    Canvas = surface.Canvas,
                    Dpi = SkiaPlatform.DefaultDpi * scaling,
                    VisualBrushRenderer = visualBrushRenderer,
                    DisableTextLcdRendering = true
                };

                return new DrawingContextImpl(nfo, Disposable.Create(() =>
                {
                    
                    surface.Canvas.Flush();
                    surface.Dispose();
                    renderTarget.Dispose();
                    _grContext.Flush();
                    session.Dispose();
                }));
            }
        }
    }
}
