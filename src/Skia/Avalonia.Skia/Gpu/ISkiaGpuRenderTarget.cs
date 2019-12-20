// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Custom Skia render target.
    /// </summary>
    public interface ISkiaGpuRenderTarget : IDisposable
    {
        /// <summary>
        /// Start rendering to this render target.
        /// </summary>
        /// <returns></returns>
        ISkiaGpuRenderSession BeginRenderingSession();
        
        bool IsCorrupted { get; }
    }
}
