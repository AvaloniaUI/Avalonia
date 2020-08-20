using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a metric for a <see cref="TextLine"/> objects,
    /// that holds information about ascent, descent, line gap, size and origin of the text line.
    /// </summary>
    public readonly struct TextLineMetrics
    {
        public TextLineMetrics(Size size, double textBaseline, TextRange textRange, bool hasOverflowed)
        {
            Size = size;
            TextBaseline = textBaseline;
            TextRange = textRange;
            HasOverflowed = hasOverflowed;
        }

        /// <summary>
        /// Gets the text range that is covered by the text line.
        /// </summary>
        /// <value>
        /// The text range that is covered by the text line.
        /// </value>
        public TextRange TextRange { get; }

        /// <summary>
        /// Gets the size of the text line.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public Size Size { get; }

        /// <summary>
        /// Gets the distance from the top to the baseline of the line of text.
        /// </summary>
        public double TextBaseline { get; }

        /// <summary>
        /// Gets a boolean value that indicates whether content of the line overflows 
        /// the specified paragraph width.
        /// </summary>
        public bool HasOverflowed { get; }

        /// <summary>
        /// Creates the text line metrics.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <param name="textRange">The text range that is covered by the text line.</param>
        /// <param name="paragraphWidth">The paragraph width.</param>
        /// <param name="paragraphProperties">The text alignment.</param>
        /// <returns></returns>
        public static TextLineMetrics Create(IEnumerable<TextRun> textRuns, TextRange textRange, double paragraphWidth,
            TextParagraphProperties paragraphProperties)
        {
            var lineWidth = 0.0;
            var ascent = 0.0;
            var descent = 0.0;
            var lineGap = 0.0;

            foreach (var textRun in textRuns)
            {
                var shapedRun = (ShapedTextCharacters)textRun;

                var fontMetrics =
                    new FontMetrics(shapedRun.Properties.Typeface, shapedRun.Properties.FontRenderingEmSize);

                lineWidth += shapedRun.Bounds.Width;

                if (ascent > fontMetrics.Ascent)
                {
                    ascent = fontMetrics.Ascent;
                }

                if (descent < fontMetrics.Descent)
                {
                    descent = fontMetrics.Descent;
                }

                if (lineGap < fontMetrics.LineGap)
                {
                    lineGap = fontMetrics.LineGap;
                }
            }

            var size = new Size(lineWidth,
                double.IsNaN(paragraphProperties.LineHeight) || MathUtilities.IsZero(paragraphProperties.LineHeight) ?
                    descent - ascent + lineGap :
                    paragraphProperties.LineHeight);

            return new TextLineMetrics(size, -ascent, textRange, size.Width > paragraphWidth);
        }
    }
}
