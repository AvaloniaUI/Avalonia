namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A metric that holds information about text specific measurements.
    /// </summary>
    public readonly record struct TextMetrics
    {
        public TextMetrics(IGlyphTypeface glyphTypeface, double fontRenderingEmSize)
        {
            var fontMetrics = glyphTypeface.Metrics;

            var scale = fontRenderingEmSize / fontMetrics.DesignEmHeight;

            FontRenderingEmSize = fontRenderingEmSize;

            Ascent = fontMetrics.Ascent * scale;

            Descent = fontMetrics.Descent * scale;

            LineGap = fontMetrics.LineGap * scale;

            LineHeight = Descent - Ascent + LineGap;

            UnderlineThickness = fontMetrics.UnderlineThickness * scale;

            UnderlinePosition = fontMetrics.UnderlinePosition * scale;

            StrikethroughThickness = fontMetrics.StrikethroughThickness * scale;

            StrikethroughPosition = fontMetrics.StrikethroughPosition * scale;
        }

        /// <summary>
        /// Em size of font used to format and display text
        /// </summary>
        public double FontRenderingEmSize { get; }

        /// <summary>
        /// Gets the recommended distance above the baseline.
        /// </summary>
        public double Ascent { get; }

        /// <summary>
        /// Gets the recommended distance under the baseline.
        /// </summary>
        public double Descent { get; }

        /// <summary>
        /// Gets the recommended additional space between two lines of text.
        /// </summary>
        public double LineGap { get; }

        /// <summary>
        /// Gets the estimated line height.
        /// </summary>
        public double LineHeight { get; }

        /// <summary>
        /// Gets a value that indicates the thickness of the underline.
        /// </summary>
        public double UnderlineThickness { get; }

        /// <summary>
        /// Gets a value that indicates the distance of the underline from the baseline.
        /// </summary>
        public double UnderlinePosition { get; }

        /// <summary>
        /// Gets a value that indicates the thickness of the underline.
        /// </summary>
        public double StrikethroughThickness { get; }

        /// <summary>
        /// Gets a value that indicates the distance of the strikethrough from the baseline.
        /// </summary>
        public double StrikethroughPosition { get; }
    }
}
