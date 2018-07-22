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

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            // TODO: These are hard coded for now. Don't do that.
            // TODO: Recreate the surface when the size changes.
             if (_surface == null)
             {
                var (width, height) = _context.GetFramebufferSize();

                _surface = SKSurface.Create(_grContext, new GRBackendRenderTargetDesc
                {
                    Height = (int)height * 2,
                    Width = (int)width * 2,
                    SampleCount = 1,
                    StencilBits = 8,
                    Config = GRPixelConfig.Bgra8888,
                    Origin = GRSurfaceOrigin.BottomLeft,
                    RenderTargetHandle = IntPtr.Zero
                });

                _canvas = _surface.Canvas;
            }

            _canvas.RestoreToCount(-1);
            _canvas.ResetMatrix();

            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Canvas = _canvas,
                Dpi = new Vector(192, 192),
                VisualBrushRenderer = visualBrushRenderer,
                DisableTextLcdRendering = true
            };

            return new DrawingContextImpl(createInfo, Disposable.Create(() =>
            {
                _grContext.Flush();
                _context.Present(); // Swap Buffers
            }));
        }

        public void Dispose()
        {
            _grContext.Dispose();
        }
    }
}
