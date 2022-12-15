using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Platform;
using Avalonia.Skia;

namespace Avalonia.Browser.Skia
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

        public void Dispose()
        {
            
        }

	public object? TryGetFeature(Type t) => null;
        
        public bool IsLost => false;
        
        public IDisposable EnsureCurrent()
        {
            return Disposable.Empty;
        }
    }

    class BrowserSkiaGraphics : IPlatformGraphics
    {
        private BrowserSkiaGpu _skia = new();
        public bool UsesSharedContext => true;
        public IPlatformGraphicsContext CreateContext() => throw new NotSupportedException();

        public IPlatformGraphicsContext GetSharedContext() => _skia;
    }
}
