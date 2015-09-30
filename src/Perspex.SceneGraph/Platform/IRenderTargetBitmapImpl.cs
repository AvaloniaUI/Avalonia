// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a
    /// <see cref="Perspex.Media.Imaging.RenderTargetBitmap"/>.
    /// </summary>
    public interface IRenderTargetBitmapImpl : IBitmapImpl, IRenderTarget
    {
    }
}
