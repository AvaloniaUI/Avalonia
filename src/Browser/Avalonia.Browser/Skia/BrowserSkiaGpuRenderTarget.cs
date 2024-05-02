using System;
using Avalonia.Browser.Rendering;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Browser.Skia
{
    internal class BrowserSkiaGpuRenderTarget : ISkiaGpuRenderTarget
    {
        private readonly GRBackendRenderTarget _renderTarget;
        private readonly BrowserGlSurface _browserGlSurface;
        private readonly PixelSize _size;

        public BrowserSkiaGpuRenderTarget(BrowserGlSurface browserGlSurface)
        {
            _size = browserGlSurface.RenderSize;

            var glFbInfo = new GRGlFramebufferInfo(browserGlSurface.GlInfo.FboId, browserGlSurface.PixelFormat.ToSkColorType().ToGlSizedFormat());
            _browserGlSurface = browserGlSurface;
            _renderTarget = new GRBackendRenderTarget(
                _size.Width,
                _size.Height,
                browserGlSurface.GlInfo.Samples,
                browserGlSurface.GlInfo.Stencils, glFbInfo);
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }

        public ISkiaGpuRenderSession BeginRenderingSession()
        {
            return new BrowserSkiaGpuRenderSession(_browserGlSurface, _renderTarget);
        }

        public bool IsCorrupted => _browserGlSurface.RenderSize != _size;
    }
}
