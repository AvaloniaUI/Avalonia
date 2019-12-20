// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.OpenGL.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Custom Skia gpu instance.
    /// </summary>
    public interface ISkiaGpu
    {
        /// <summary>
        /// Attempts to create custom render target from given surfaces.
        /// </summary>
        /// <param name="surfaces">Surfaces.</param>
        /// <returns>Created render target or <see langword="null"/> if it fails.</returns>
        ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces);
    }

    public interface IOpenGlAwareSkiaGpu : ISkiaGpu
    {
        IOpenGlTextureBitmapImpl CreateOpenGlTextureBitmap();
    }
}
