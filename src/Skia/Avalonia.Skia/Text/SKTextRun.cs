// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;

using SkiaSharp;

namespace Avalonia.Skia.Text
{
    public class SKTextRun
    {
        public SKTextRun(
            SKTextPointer textPointer,
            SKGlyphRun glyphRun,
            SKTextFormat textFormat,
            SKFontMetrics fontMetrics,
            float width,
            IBrush foreground = null)
        {
            TextPointer = textPointer;
            GlyphRun = glyphRun;
            TextFormat = textFormat;
            FontMetrics = fontMetrics;
            Width = width;
            Foreground = foreground;
        }

        /// <summary>
        /// Gets the text pointer.
        /// </summary>
        /// <value>
        /// The text pointer.
        /// </value>
        public SKTextPointer TextPointer { get; }

        /// <summary>
        /// Gets the glyph run.
        /// </summary>
        /// <value>
        /// The glyphs.
        /// </value>
        public SKGlyphRun GlyphRun { get; }

        /// <summary>
        /// Gets the text format.
        /// </summary>
        /// <value>
        /// The text format.
        /// </value>
        public SKTextFormat TextFormat { get; }

        /// <summary>
        /// Gets the font metrics.
        /// </summary>
        /// <value>
        /// The font metrics.
        /// </value>
        public SKFontMetrics FontMetrics { get; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        public float Width { get; }

        /// <summary>
        /// Gets the foreground.
        /// </summary>
        /// <value>
        /// The drawing effect.
        /// </value>
        public IBrush Foreground { get; }
    }
}
