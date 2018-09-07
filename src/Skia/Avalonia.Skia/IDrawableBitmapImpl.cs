// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Extended bitmap implementation that allows for drawing it's contents.
    /// </summary>
    internal interface IDrawableBitmapImpl : IBitmapImpl
    {
        /// <summary>
        /// Draw bitmap to a drawing context.
        /// </summary>
        /// <param name="context">Drawing context.</param>
        /// <param name="sourceRect">Source rect.</param>
        /// <param name="destRect">Destination rect.</param>
        /// <param name="paint">Paint to use.</param>
        void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint);
    }
}
