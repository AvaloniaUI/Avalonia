// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a line of text that is used for text rendering.
    /// </summary>
    public abstract class TextLine
    {
        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>
        /// The text pointer.
        /// </value>
        public abstract TextPointer Text { get; }

        /// <summary>
        /// Gets the text runs.
        /// </summary>
        /// <value>
        /// The text runs.
        /// </value>
        public abstract IReadOnlyList<TextRun> TextRuns { get; }

        /// <summary>
        /// Gets the line metrics.
        /// </summary>
        /// <value>
        /// The line metrics.
        /// </value>
        public abstract TextLineMetrics LineMetrics { get; }

        /// <summary>
        /// Draws the <see cref="TextLine"/> at the given origin.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="origin">The origin.</param>
        public abstract void Draw(IDrawingContextImpl drawingContext, Point origin);

        /// <summary>
        /// Client to get the character hit corresponding to the specified 
        /// distance from the beginning of the line.
        /// </summary>
        /// <param name="distance">distance in text flow direction from the beginning of the line</param>
        /// <returns>The <see cref="CharacterHit"/></returns>
        public abstract CharacterHit GetCharacterHitFromDistance(double distance);

        /// <summary>
        /// Client to get the distance from the beginning of the line from the specified 
        /// <see cref="CharacterHit"/>.
        /// </summary>
        /// <param name="characterHit"><see cref="CharacterHit"/> of the character to query the distance.</param>
        /// <returns>Distance in text flow direction from the beginning of the line.</returns>
        public abstract double GetDistanceFromCharacterHit(CharacterHit characterHit);

        /// <summary>
        /// Client to get the next <see cref="CharacterHit"/> for caret navigation.
        /// </summary>
        /// <param name="characterHit">The current <see cref="CharacterHit"/>.</param>
        /// <returns>The next <see cref="CharacterHit"/>.</returns>
        public abstract CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit);

        /// <summary>
        /// Client to get the previous character hit for caret navigation
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>The previous <see cref="CharacterHit"/></returns>
        public abstract CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit);

        /// <summary>
        /// Client to get the previous character hit after backspacing
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>The <see cref="CharacterHit"/> after backspacing</returns>
        public abstract CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit);

        /// <summary>
        /// Gets the text line offset x.
        /// </summary>
        /// <param name="lineWidth">The line width.</param>
        /// <param name="paragraphWidth">The paragraph width.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <returns>The paragraph offset.</returns>
        internal static double GetParagraphOffsetX(double lineWidth, double paragraphWidth, TextAlignment textAlignment)
        {
            if (double.IsPositiveInfinity(paragraphWidth))
            {
                return 0;
            }

            switch (textAlignment)
            {
                case TextAlignment.Center:
                    return (paragraphWidth - lineWidth) / 2;

                case TextAlignment.Right:
                    return paragraphWidth - lineWidth;

                default:
                    return 0.0f;
            }
        }
    }
}
