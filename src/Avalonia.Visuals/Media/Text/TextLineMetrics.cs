// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Media.Text
{
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
        ///     Gets the overall recommended distance above the baseline.
        /// </summary>
        /// <value>
        ///     The ascent.
        /// </value>
        public double Ascent { get; }

        /// <summary>
        ///     Gets the overall recommended distance under the baseline.
        /// </summary>
        /// <value>
        ///     The descent.
        /// </value>
        public double Descent { get; }

        /// <summary>
        ///     Gets the overall recommended additional space between two lines of text.
        /// </summary>
        /// <value>
        ///     The leading.
        /// </value>
        public double LineGap { get; }

        /// <summary>
        ///     Gets the size of the text line.
        /// </summary>
        /// <value>
        ///     The size.
        /// </value>
        public Size Size { get; }

        /// <summary>
        ///     Gets the baseline origin.
        /// </summary>
        /// <value>
        ///     The baseline origin.
        /// </value>
        public Point BaselineOrigin { get; } //ToDo: Remove this and instead calculate the baseline on demand when drawing.

        /// <summary>
        ///     Creates the text line metrics.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <param name="paragraphWidth"></param>
        /// <param name="textAlignment"></param>
        /// <returns></returns>
        public static TextLineMetrics Create(IEnumerable<TextRun> textRuns, double paragraphWidth, TextAlignment textAlignment)
        {
            var lineWidth = 0.0;
            var ascent = 0.0;
            var descent = 0.0;
            var lineGap = 0.0;

            foreach (var textRun in textRuns)
            {
                UpdateTextLineMetrics(textRun, ref lineWidth, ref ascent, ref descent, ref lineGap);
            }

            var xOrigin = TextLine.GetParagraphOffsetX(lineWidth, paragraphWidth, textAlignment);

            return new TextLineMetrics(lineWidth, xOrigin, ascent, descent, lineGap);
        }

        private static void UpdateTextLineMetrics(TextRun textRun, ref double width, ref double ascent, ref double descent, ref double lineGap)
        {
            width += textRun.GlyphRun.Bounds.Width;

            if (ascent > textRun.TextFormat.FontMetrics.Ascent)
            {
                ascent = textRun.TextFormat.FontMetrics.Ascent;
            }

            if (descent < textRun.TextFormat.FontMetrics.Descent)
            {
                descent = textRun.TextFormat.FontMetrics.Descent;
            }

            if (lineGap < textRun.TextFormat.FontMetrics.LineGap)
            {
                lineGap = textRun.TextFormat.FontMetrics.LineGap;
            }
        }
    }
}
