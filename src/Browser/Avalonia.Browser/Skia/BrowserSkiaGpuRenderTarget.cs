using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Browser.Skia
{
    internal class BrowserSkiaGpuRenderTarget : ISkiaGpuRenderTarget
    {
        private readonly GRBackendRenderTarget _renderTarget;
        private readonly BrowserSkiaSurface _browserSkiaSurface;
        private readonly PixelSize _size;

        public BrowserSkiaGpuRenderTarget(BrowserSkiaSurface browserSkiaSurface)
        {
            _size = browserSkiaSurface.Size;

            var glFbInfo = new GRGlFramebufferInfo(browserSkiaSurface.GlInfo.FboId, browserSkiaSurface.ColorType.ToGlSizedFormat());
            _browserSkiaSurface = browserSkiaSurface;
            _renderTarget = new GRBackendRenderTarget(
                (int)(browserSkiaSurface.Size.Width * browserSkiaSurface.Scaling),
                (int)(browserSkiaSurface.Size.Height * browserSkiaSurface.Scaling),
                browserSkiaSurface.GlInfo.Samples,
                browserSkiaSurface.GlInfo.Stencils, glFbInfo);
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }

        public ISkiaGpuRenderSession BeginRenderingSession()
        {
            return new BrowserSkiaGpuRenderSession(_browserSkiaSurface, _renderTarget);
        }

        public bool IsCorrupted => _browserSkiaSurface.Size != _size;
    }
}
