using System;
using Avalonia.Metadata;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Custom render session for Skia render target.
    /// </summary>
    public interface ISkiaGpuRenderSession : IDisposable
    {
        /// <summary>
        /// GrContext used by this session.
        /// </summary>
        GRContext GrContext { get; }

        /// <summary>
        /// Canvas that will be used to render.
        /// </summary>
        SKSurface SkSurface { get; }

        /// <summary>
        /// Scaling factor.
        /// </summary>
        double ScaleFactor { get; }
        
        GRSurfaceOrigin SurfaceOrigin { get; }
    }

    [PrivateApi]
    public interface ISkiaGpuRenderSessionWithPartialPresentation : ISkiaGpuRenderSession
    {
        IPartialPresentationFeature? PartialPresentation { get; }
    }
}
