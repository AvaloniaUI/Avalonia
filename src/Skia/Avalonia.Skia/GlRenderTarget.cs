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

            if (_surface == null || (_desc.Width != (int)width * 2 || _desc.Height != (int)height * 2))
            {
                _desc = new GRBackendRenderTargetDesc
                {
                    Height = (int)height * 2,
                    Width = (int)width * 2,
                    SampleCount = 1,
                    StencilBits = 8,
                    Config = GRPixelConfig.Bgra8888,
                    Origin = GRSurfaceOrigin.BottomLeft,
                    RenderTargetHandle = IntPtr.Zero
                };

                _surface?.Dispose();
                _surface = SKSurface.Create(_grContext, _desc);
                _canvas = _surface.Canvas;
            }


            _canvas.Clear(SKColors.Orange);
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
            _canvas.Dispose();
            _surface.Dispose();
            _grContext.Dispose();
        }
    }
}
