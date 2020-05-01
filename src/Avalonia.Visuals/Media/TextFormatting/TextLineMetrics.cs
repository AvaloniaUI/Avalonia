using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a metric for a <see cref="TextLine"/> objects,
    /// that holds information about ascent, descent, line gap, size and origin of the text line.
    /// </summary>
    public readonly struct TextLineMetrics
    {
        public TextLineMetrics(double width, double xOrigin, double ascent, double descent, double lineGap)
        {
            Ascent = ascent;
            Descent = descent;
            LineGap = lineGap;
            Size = new Size(width, descent - ascent + lineGap);
            BaselineOrigin = new Point(xOrigin, -ascent);
        }

        /// <summary>
        /// Gets the overall recommended distance above the baseline.
        /// </summary>
        /// <value>
        /// The ascent.
        /// </value>
        public double Ascent { get; }

        /// <summary>
        /// Gets the overall recommended distance under the baseline.
        /// </summary>
        /// <value>
        /// The descent.
        /// </value>
        public double Descent { get; }

        /// <summary>
        /// Gets the overall recommended additional space between two lines of text.
        /// </summary>
        /// <value>
        /// The leading.
        /// </value>
        public double LineGap { get; }

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
        /// <param name="paragraphWidth">The paragraph width.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <returns></returns>
        public static TextLineMetrics Create(IEnumerable<TextRun> textRuns, double paragraphWidth, TextAlignment textAlignment)
        {
            var lineWidth = 0.0;
            var ascent = 0.0;
            var descent = 0.0;
            var lineGap = 0.0;

            foreach (var textRun in textRuns)
            {
                var shapedRun = (ShapedTextRun)textRun;

                lineWidth += shapedRun.Bounds.Width;

                var textFormat = textRun.Style.TextFormat;

                if (ascent > textRun.Style.TextFormat.FontMetrics.Ascent)
                {
                    ascent = textFormat.FontMetrics.Ascent;
                }

                if (descent < textFormat.FontMetrics.Descent)
                {
                    descent = textFormat.FontMetrics.Descent;
                }

                if (lineGap < textFormat.FontMetrics.LineGap)
                {
                    lineGap = textFormat.FontMetrics.LineGap;
                }
            }

            var xOrigin = TextLine.GetParagraphOffsetX(lineWidth, paragraphWidth, textAlignment);

            return new TextLineMetrics(lineWidth, xOrigin, ascent, descent, lineGap);
        }
    }
}
