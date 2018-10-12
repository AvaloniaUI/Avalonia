// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;

using SkiaSharp;

namespace Avalonia.Skia
{
    public class SKTextRun
    {
        public SKTextRun(string text, byte[] characterCodePoints, SKTextFormat textFormat, SKFontMetrics fontMetrics, float width, IBrush drawingEffect = null)
        {
            Text = text;
            TextFormat = textFormat;
            FontMetrics = fontMetrics;
            Width = width;
            CharacterCodePoints = characterCodePoints;
            DrawingEffect = drawingEffect;
        }

        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; }

        /// <summary>
        /// Gets the character code points.
        /// </summary>
        /// <value>
        /// The character code points.
        /// </value>
        public byte[] CharacterCodePoints { get; }

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
        /// Gets the drawing effect.
        /// </summary>
        /// <value>
        /// The drawing effect.
        /// </value>
        public IBrush DrawingEffect { get; }
    }
}
