using System;
using System.Collections.Generic;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Custom Skia gpu instance.
    /// </summary>
    public interface ISkiaGpu : IPlatformGraphicsContext
    {
        /// <summary>
        /// Skia's GrContext
        /// </summary>
        GRContext GrContext { get; }
        
        /// <summary>
        /// Attempts to create custom render target from given surfaces.
        /// </summary>
        /// <param name="surfaces">Surfaces.</param>
        /// <returns>Created render target or <see langword="null"/> if it fails.</returns>
        ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces);

        /// <summary>
        /// Creates an offscreen render target surface
        /// </summary>
        /// <param name="size">size in pixels.</param>
        /// <param name="surfaceOrigin">The expected surface origin</param>
        ISkiaSurface TryCreateSurface(PixelSize size, GRSurfaceOrigin? surfaceOrigin);
    }
    
    public interface ISkiaSurface : IDisposable
    {
        SKSurface Surface { get; }
        bool CanBlit { get; }
        void Blit(SKCanvas canvas);
    }
}
