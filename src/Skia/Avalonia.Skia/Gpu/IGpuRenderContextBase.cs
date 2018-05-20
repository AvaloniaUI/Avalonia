// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
}