using System;
using Avalonia.Platform.Gpu;
using SkiaSharp;

namespace Avalonia.Skia.Gpu
{
    /// <summary>
    /// EGL render context base. Can be either window or offscreen based.
    /// </summary>
    public class EGLRenderContextBase : IGpuRenderContextBase
    {
        /// <summary>
        /// Backing platform.
        /// </summary>
        protected IEGLPlatform Platform { get; }

        /// <summary>
        /// Create new instance of EGLRenderContextBase.
        /// </summary>
        /// <param name="platform">Platform to use.</param>
        /// <param name="context">Context to use.</param>
        public EGLRenderContextBase(IEGLPlatform platform, GRContext context)
        {
            Platform = platform ?? throw new ArgumentNullException(nameof(platform));
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public GRContext Context { get; }

        /// <inheritdoc />
        public virtual bool PrepareForRendering()
        {
            return true; // I think we don't need to set context current, need to test it.
        }
    }
}