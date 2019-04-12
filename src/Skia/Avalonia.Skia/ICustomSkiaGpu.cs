// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Custom Skia gpu instance.
    /// </summary>
    public interface ICustomSkiaGpu
    {
        /// <summary>
        /// Skia GrContext used.
        /// </summary>
        GRContext GrContext { get; }

        /// <summary>
        /// Attempts to create custom render target from given surfaces.
        /// </summary>
        /// <param name="surfaces">Surfaces.</param>
        /// <returns>Created render target or <see langword="null"/> if it fails.</returns>
        ICustomSkiaRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces);
    }
}
