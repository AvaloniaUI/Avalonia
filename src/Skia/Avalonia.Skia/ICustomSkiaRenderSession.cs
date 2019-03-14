using System;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Custom render session for Skia render target.
    /// </summary>
    public interface ICustomSkiaRenderSession : IDisposable
    {
        /// <summary>
        /// GrContext used by this session.
        /// </summary>
        GRContext GrContext { get; }

        /// <summary>
        /// Canvas that will be used to render.
        /// </summary>
        SKCanvas Canvas { get; }

        /// <summary>
        /// Scaling factor.
        /// </summary>
        double ScaleFactor { get; }
    }
}
