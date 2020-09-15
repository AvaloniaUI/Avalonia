using System;
using System.Collections.Generic;
using Avalonia.OpenGL.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    public interface IControlledSurface : IDisposable
    {
        public SKSurface Surface { get; }
    }

    /// <summary>
    /// Custom Skia gpu instance.
    /// </summary>
    public interface ISkiaGpu
    {
        /// <summary>
        /// Attempts to create custom render target from given surfaces.
        /// </summary>
        /// <param name="surfaces">Surfaces.</param>
        /// <returns>Created render target or <see langword="null"/> if it fails.</returns>
        ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces);

        IControlledSurface CreateControlledSurface(PixelSize size);
    }

    public interface IOpenGlAwareSkiaGpu : ISkiaGpu
    {
        IOpenGlTextureBitmapImpl CreateOpenGlTextureBitmap();
    }
}
