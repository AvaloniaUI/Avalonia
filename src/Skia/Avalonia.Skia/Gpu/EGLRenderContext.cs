// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform.Gpu;
using SkiaSharp;

namespace Avalonia.Skia.Gpu
{
    /// <summary>
    /// EGL render context.
    /// </summary>
    public class EGLRenderContext : EGLRenderContextBase, IGpuRenderContext
    {
        private readonly IEGLSurface _surface;
        
        public EGLRenderContext(IEGLSurface surface, IEGLPlatform platform, GRContext context)
            : base(platform, context)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            PrepareForRendering();

            Platform.DestroySurface(_surface);
        }

        /// <inheritdoc />
        public FramebufferParameters GetFramebufferParameters()
        {
            return _surface.GetFramebufferParameters();
        }

        /// <inheritdoc />
        public override bool PrepareForRendering()
        {
            return Platform.MakeCurrent(_surface);
        }

        /// <inheritdoc />
        public bool Present()
        {
            return Platform.SwapBuffers(_surface);
        }

        /// <inheritdoc />
        public Size GetFramebufferSize()
        {
            var (width, height) = _surface.GetSize();

            return new Size(width, height);
        }

        /// <inheritdoc />
        public Size GetFramebufferDpi()
        {
            var (x, y) = _surface.GetDpi();

            return new Size(x, y);
        }
    }
}