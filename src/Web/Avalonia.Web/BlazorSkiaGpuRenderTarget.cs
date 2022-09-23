using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Web.Blazor
{
    internal class BlazorSkiaGpuRenderTarget : ISkiaGpuRenderTarget
    {
        private readonly GRBackendRenderTarget _renderTarget;
        private readonly BlazorSkiaSurface _blazorSkiaSurface;
        private readonly PixelSize _size;

        public BlazorSkiaGpuRenderTarget(BlazorSkiaSurface blazorSkiaSurface)
        {
            _size = blazorSkiaSurface.Size;

            var glFbInfo = new GRGlFramebufferInfo(blazorSkiaSurface.GlInfo.FboId, blazorSkiaSurface.ColorType.ToGlSizedFormat());
            {
                _blazorSkiaSurface = blazorSkiaSurface;
                _renderTarget = new GRBackendRenderTarget(
                    (int)(blazorSkiaSurface.Size.Width * blazorSkiaSurface.Scaling),
                    (int)(blazorSkiaSurface.Size.Height * blazorSkiaSurface.Scaling),
                    blazorSkiaSurface.GlInfo.Samples,
                    blazorSkiaSurface.GlInfo.Stencils, glFbInfo);
            }
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }

        public ISkiaGpuRenderSession BeginRenderingSession()
        {
            return new BlazorSkiaGpuRenderSession(_blazorSkiaSurface, _renderTarget);
        }

        public bool IsCorrupted => _blazorSkiaSurface.Size != _size;
    }
}
