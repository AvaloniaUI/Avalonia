namespace Avalonia.Media
{
    /// <summary>
    /// Describes the formatting for a span of text in a <see cref="FormattedText"/> object.
    /// </summary>
    public class FormattedTextStyleSpan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedTextStyleSpan"/> class.
        /// </summary>
        /// <param name="startIndex">The index of the first character in the span.</param>
        /// <param name="length">The length of the span.</param>
        /// <param name="fontFamily">The span's font family.</param>
        /// <param name="fontSize">The span's font size.</param>
        /// <param name="fontStyle">The span's font style.</param>
        /// <param name="fontWeight">The span's font weight</param>
        /// <param name="foreground">The span's foreground brush.</param>
        public FormattedTextStyleSpan(
            int startIndex,
            int length,
            FontFamily fontFamily = null,
            double? fontSize = null,
            FontStyle? fontStyle = null,
            FontWeight? fontWeight = null,
            IBrush foreground = null)
        {
            StartIndex = startIndex;
            Length = length;
            Typeface = GetTypeface(fontFamily, fontWeight, fontStyle);
            FontSize = fontSize;
            Foreground = foreground;
        }

        /// <summary>
        /// Gets the index of the first character in the span.
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// Gets the length of the span.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the typeface of the span.
        /// </summary>
        public Typeface Typeface { get; }

        /// <summary>
        /// Gets the font size, in device independent pixels.
        /// </summary>
        public double? FontSize { get; }

        /// <summary>
        /// Gets the span's foreground brush.
        /// </summary>
        public IBrush Foreground { get; }

        private static Typeface GetTypeface(FontFamily fontFamily, FontWeight? fontWeight, FontStyle? fontStyle)
        {
            if (fontFamily == null && fontWeight == null && fontStyle == null)
            {
                return null;
            }

            if (fontFamily == null)
            {
                fontFamily = FontFamily.Default;
            }

            if (fontWeight == null)
            {
                fontWeight = FontWeight.Normal;
            }

            if (fontStyle == null)
            {
                fontStyle = FontStyle.Normal;
            }

            return new Typeface(fontFamily, fontStyle.Value, fontWeight.Value);
        }
    }
}
