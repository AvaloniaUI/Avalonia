using System;
using Avalonia.Metadata;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Custom Skia render target.
    /// </summary>
    //TODO12: [PrivateApi]
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

    //TODO12: Merge with ISkiaGpuRenderTarget
    internal interface ISkiaGpuRenderTargetWithProperties : ISkiaGpuRenderTarget
    {
        RenderTargetProperties Properties { get; }
    }
}
