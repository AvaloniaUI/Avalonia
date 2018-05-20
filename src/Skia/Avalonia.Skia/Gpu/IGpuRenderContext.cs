// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Gpu;

namespace Avalonia.Skia.Gpu
{
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

        /// <summary>
        /// Get dpi of window framebuffer.
        /// </summary>
        /// <returns>Dpi of a framebuffer</returns>
        Size GetFramebufferDpi();

        /// <summary>
        /// Recreate backing surface.
        /// </summary>
        void RecreateSurface();
    }
}