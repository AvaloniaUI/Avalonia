using System.Collections.Generic;
using Avalonia.Skia;

namespace Avalonia.Web.Skia
{
    public class BrowserSkiaGpu : ISkiaGpu
    {
        public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                if (surface is BrowserSkiaSurface browserSkiaSurface)
                {
                    return new BrowserSkiaGpuRenderTarget(browserSkiaSurface);
                }
            }

            return null;
        }

        public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session)
        {
            return null;
        }
    }
}
