using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a line of text that is used for text rendering.
    /// </summary>
    public abstract class TextLine
    {
        /// <summary>
        /// Gets the text range that is covered by the line.
        /// </summary>
        /// <value>
        /// The text range that is covered by the line.
        /// </value>
        public abstract TextRange TextRange { get; }

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
        /// Gets the state of the line when broken by line breaking process.
        /// </summary>
        /// <returns>
        /// A <see cref="TextLineBreak"/> value that represents the line break.
        /// </returns>
        public abstract TextLineBreak TextLineBreak { get; }

        /// <summary>
        /// Gets a value that indicates whether the line is collapsed.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if the line is collapsed; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool HasCollapsed { get; }

        /// <summary>
        /// Draws the <see cref="TextLine"/> at the given origin.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="origin">The origin.</param>
        public abstract void Draw(DrawingContext drawingContext, Point origin);

        /// <summary>
        /// Create a collapsed line based on collapsed text properties.
        /// </summary>
        /// <param name="collapsingPropertiesList">A list of <see cref="TextCollapsingProperties"/>
        /// objects that represent the collapsed text properties.</param>
        /// <returns>
        /// A <see cref="TextLine"/> value that represents a collapsed line that can be displayed.
        /// </returns>
        public abstract TextLine Collapse(params TextCollapsingProperties[] collapsingPropertiesList);

        /// <summary>
        /// Gets the character hit corresponding to the specified distance from the beginning of the line.
        /// </summary>
        /// <param name="distance">A <see cref="double"/> value that represents the distance from the beginning of the line.</param>
        /// <returns>The <see cref="CharacterHit"/> object at the specified distance from the beginning of the line.</returns>
        public abstract CharacterHit GetCharacterHitFromDistance(double distance);

        /// <summary>
        /// Gets the distance from the beginning of the line to the specified character hit.
        /// <see cref="CharacterHit"/>.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> object whose distance you want to query.</param>
        /// <returns>A <see cref="double"/> that represents the distance from the beginning of the line.</returns>
        public abstract double GetDistanceFromCharacterHit(CharacterHit characterHit);

        /// <summary>
        /// Gets the next character hit for caret navigation.
        /// </summary>
        /// <param name="characterHit">The current <see cref="CharacterHit"/>.</param>
        /// <returns>The next <see cref="CharacterHit"/>.</returns>
        public abstract CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit);

        /// <summary>
        /// Gets the previous character hit for caret navigation.
        /// </summary>
        /// <param name="characterHit">The current <see cref="CharacterHit"/>.</param>
        /// <returns>The previous <see cref="CharacterHit"/>.</returns>
        public abstract CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit);

        /// <summary>
        /// Gets the previous character hit after backspacing.
        /// </summary>
        /// <param name="characterHit">The current <see cref="CharacterHit"/>.</param>
        /// <returns>The <see cref="CharacterHit"/> after backspacing.</returns>
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
