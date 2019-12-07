// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        public Point BaselineOrigin { get; }
    }
}
