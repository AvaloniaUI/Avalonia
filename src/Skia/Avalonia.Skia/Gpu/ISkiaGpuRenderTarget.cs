using System;
using Avalonia.Metadata;
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

    [PrivateApi]
    //TODO12: Merge with ISkiaGpuRenderTarget
    public interface ISkiaGpuRenderTarget2 : ISkiaGpuRenderTarget
    {
        ISkiaGpuRenderSession BeginRenderingSession(PixelSize pixelSize);
    }
}
