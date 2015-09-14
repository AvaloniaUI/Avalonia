// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a
    /// <see cref="Perspex.Media.Imaging.RenderTargetBitmap"/>.
    /// </summary>
    public interface IRenderTargetBitmapImpl : IBitmapImpl, IDisposable
    {
        /// <summary>
        /// Renders an <see cref="IVisual"/> into the bitmap.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <remarks>
        /// Before calling this method, ensure that <paramref name="visual"/> has been measured.
        /// </remarks>
        void Render(IVisual visual);
    }
}
