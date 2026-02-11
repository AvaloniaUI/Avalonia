using System;

namespace Avalonia.Skia
{
    /// <summary>
    /// Custom Skia render target.
    /// </summary>
    public interface ISkiaGpuRenderTarget : IDisposable
    {
        /// <summary>
        /// Start rendering to this render target.
        /// </summary>
        /// <param name="expectedPixelSize">The expected size.</param>
        /// <returns>A render session instance.</returns>
        ISkiaGpuRenderSession BeginRenderingSession(PixelSize? expectedPixelSize);
        
        bool IsCorrupted { get; }
    }
}
