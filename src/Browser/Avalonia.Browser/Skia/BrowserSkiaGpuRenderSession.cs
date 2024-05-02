using System;
using Avalonia.Browser.Rendering;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Browser.Skia
{
    internal class BrowserSkiaGpuRenderSession : ISkiaGpuRenderSession
    {
        private readonly SKSurface _surface;

        public BrowserSkiaGpuRenderSession(BrowserGlSurface browserGlSurface, GRBackendRenderTarget renderTarget)
        {
            _surface = SKSurface.Create(browserGlSurface.Context, renderTarget, GRSurfaceOrigin.BottomLeft, 
                browserGlSurface.PixelFormat.ToSkColorType(), new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal))
                ?? throw new InvalidOperationException("Unable to create SKSurface.");

            GrContext = browserGlSurface.Context;
            ScaleFactor = browserGlSurface.Scaling;
            SurfaceOrigin = GRSurfaceOrigin.BottomLeft;

            browserGlSurface.EnsureResize();
        }

        public void Dispose()
        {
            _surface.Flush();

            _surface.Dispose();
        }

        public GRContext GrContext { get; }

        public SKSurface SkSurface => _surface;

        public double ScaleFactor { get; }

        public GRSurfaceOrigin SurfaceOrigin { get; }
    }
}
