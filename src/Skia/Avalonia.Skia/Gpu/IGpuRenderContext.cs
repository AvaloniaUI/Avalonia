// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform.Gpu;
using SkiaSharp;

namespace Avalonia.Skia.Gpu
{
    /// <summary>
    /// Render context base for Gpu accelerated Skia rendering.
    /// </summary>
    public interface IGpuRenderContextBase
    {
        /// <summary>
        /// Skia graphics context.
        /// </summary>
        GRContext Context { get; }

        /// <summary>
        /// Prepare context for rendering commands.
        /// </summary>
        bool PrepareForRendering();
    }

    /// <summary>
    /// Render context for Gpu accelerated Skia rendering.
    /// </summary>
    public interface IGpuRenderContext : IGpuRenderContextBase, IDisposable
    {
        /// <summary>
        /// Get framebuffer parameters.
        /// </summary>
        /// <returns>Framebuffer parameters.</returns>
        FramebufferParameters GetFramebufferParameters();
        
        /// <summary>
        /// Present rendering results to framebuffer.
        /// </summary>
        bool Present();

        /// <summary>
        /// Get size of window framebuffer.
        /// </summary>
        /// <returns>Size of a framebuffer</returns>
        Size GetFramebufferSize();
    }
}