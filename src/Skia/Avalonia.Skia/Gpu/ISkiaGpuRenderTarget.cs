using System;
using SkiaSharp;

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
        /// <returns></returns>
        ISkiaGpuRenderSession BeginRenderingSession();
        
        bool IsCorrupted { get; }
    }
}
