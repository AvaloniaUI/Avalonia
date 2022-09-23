using System.Collections.Generic;
using Avalonia.Skia;

namespace Avalonia.Web.Blazor
{
    public class BlazorSkiaGpu : ISkiaGpu
    {
        public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                if (surface is BlazorSkiaSurface blazorSkiaSurface)
                {
                    return new BlazorSkiaGpuRenderTarget(blazorSkiaSurface);
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
