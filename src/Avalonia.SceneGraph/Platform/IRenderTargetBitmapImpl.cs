// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a
    /// <see cref="Avalonia.Media.Imaging.RenderTargetBitmap"/>.
    /// </summary>
    public interface IRenderTargetBitmapImpl : IBitmapImpl, IRenderTarget
    {
    }
}
