using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Web.Blazor
{
    internal class BlazorSkiaGpuRenderSession : ISkiaGpuRenderSession
    {
        private readonly SKSurface _surface;


        public BlazorSkiaGpuRenderSession(BlazorSkiaSurface blazorSkiaSurface, GRBackendRenderTarget renderTarget)
        {
            _surface = SKSurface.Create(blazorSkiaSurface.Context, renderTarget, blazorSkiaSurface.Origin, blazorSkiaSurface.ColorType);

            GrContext = blazorSkiaSurface.Context;

            ScaleFactor = blazorSkiaSurface.Scaling;

            SurfaceOrigin = blazorSkiaSurface.Origin;
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
