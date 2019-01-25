// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SkiaSharp;

namespace Avalonia.Skia.Text
{
    public class SKGlyphCluster
    {
        public SKGlyphCluster(int textPosition, int length, SKRect bounds)
        {
            TextPosition = textPosition;
            Length = length;
            Bounds = bounds;
        }

        /// <summary>
        /// Gets the text position.
        /// </summary>
        /// <value>
        /// The text position.
        /// </value>
        public int TextPosition { get; }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public int Length { get; }

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        public SKRect Bounds { get; }
    }
}
