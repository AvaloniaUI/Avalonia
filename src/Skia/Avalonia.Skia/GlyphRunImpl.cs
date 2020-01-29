// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <inheritdoc />
    public class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(SKPaint paint, SKTextBlob textBlob)
        {
            Paint = paint;
            TextBlob = textBlob;
        }

        /// <summary>
        ///     Gets the paint to draw with.
        /// </summary>
        public SKPaint Paint { get; }

        /// <summary>
        ///     Gets the text blob to draw.
        /// </summary>
        public SKTextBlob TextBlob { get; }

        void IDisposable.Dispose()
        {
            TextBlob.Dispose();
            Paint.Dispose();
        }
    }
}
