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
        public TextLineMetrics(Size size, Point baselineOrigin, TextRange textRange)
        {
            Size = size;
            BaselineOrigin = baselineOrigin;
            TextRange = textRange;
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
        /// Gets the baseline origin.
        /// </summary>
        /// <value>
        /// The baseline origin.
        /// </value>
        public Point BaselineOrigin { get; }

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

            var xOrigin = TextLine.GetParagraphOffsetX(lineWidth, paragraphWidth, paragraphProperties.TextAlignment);

            var baselineOrigin = new Point(xOrigin, -ascent);

            var size = new Size(lineWidth,
                double.IsNaN(paragraphProperties.LineHeight) || MathUtilities.IsZero(paragraphProperties.LineHeight) ?
                    descent - ascent + lineGap :
                    paragraphProperties.LineHeight);

            return new TextLineMetrics(size, baselineOrigin, textRange);
        }
    }
}
