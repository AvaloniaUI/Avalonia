using System;
using System.Reactive.Disposables;
using Avalonia.Gpu;
using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;
using static Avalonia.Skia.SurfaceRenderTarget;

namespace Avalonia.Skia
{
    public class GlRenderTarget : IRenderTarget
    {
        private readonly GRContext _grContext;
        private readonly IGpuContext _context;

        public GlRenderTarget(IGpuContext context)
        {
            _context = context;

            // TODO: Choose GLES or GL based on some sort of configuration.
            _grContext = GRContext.Create(GRBackend.OpenGL, GRGlInterface.AssembleGlInterface((_, name) =>
            {
                return _context.GetProcAddress(name);
            }));
        }

        SKSurface _surface;
        SKCanvas _canvas;
        GRBackendRenderTargetDesc _desc;

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var (width, height) = _context.GetFramebufferSize();
            var (dpiX, dpiY) = _context.GetDpi();

            width *= dpiX;
            height *= dpiY;

            if (_surface == null || (_desc.Width != (int)width|| _desc.Height != (int)height))
            {
                _context.ResizeContext(width, height);
                _desc = new GRBackendRenderTargetDesc
                {
                    Height = (int)height,
                    Width = (int)width,
                    // TODO: Get these framebuffer parameters from the GLContext
                    SampleCount = 1,
                    StencilBits = 8,
                    // TODO: Use the platform's preferred pixel format to reduce internal conversions
                    Config = GRPixelConfig.Bgra8888,

                    Origin = GRSurfaceOrigin.BottomLeft,

                    // TODO: Get the FBO ID rather than assuming zero here. 
                    RenderTargetHandle = IntPtr.Zero
                };

                _canvas?.Dispose();
                _surface?.Dispose();
                _surface = SKSurface.Create(_grContext, _desc);
                _canvas = _surface.Canvas;
            }

            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Canvas = _canvas,
                Dpi = new Vector(dpiX * SkiaPlatform.DefaultDpi.X, dpiY * SkiaPlatform.DefaultDpi.Y),
                VisualBrushRenderer = visualBrushRenderer,
                DisableTextLcdRendering = true
            };

            return new DrawingContextImpl(createInfo, Disposable.Create(() =>
            {
                _grContext.Flush();
                _context.Present();
            }));
        }

        public void Dispose()
        {
            _canvas.Dispose();
            _surface.Dispose();
            _grContext.Dispose();
        }
    }
}
