using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Browser.Skia
{
    internal class BrowserSkiaGpuRenderSession : ISkiaGpuRenderSession
    {
        private readonly SKSurface _surface;

        public BrowserSkiaGpuRenderSession(BrowserSkiaSurface browserSkiaSurface, GRBackendRenderTarget renderTarget)
        {
            _surface = SKSurface.Create(browserSkiaSurface.Context, renderTarget, browserSkiaSurface.Origin, 
                browserSkiaSurface.ColorType, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));

            GrContext = browserSkiaSurface.Context;

            ScaleFactor = browserSkiaSurface.Scaling;

            SurfaceOrigin = browserSkiaSurface.Origin;
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
