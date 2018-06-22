using System;

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
        /// <param name="foregroundBrush">The span's foreground brush.</param>
        public FormattedTextStyleSpan(
            int startIndex,
            int length,
            FontFamily fontFamily = null,
            double? fontSize = null,
            FontStyle? fontStyle = null,
            FontWeight? fontWeight = null,
            IBrush foregroundBrush = null)
        {
            StartIndex = startIndex;
            Length = length;
            FontFamily = fontFamily;
            FontSize = fontSize;
            FontStyle = fontStyle;
            FontWeight = fontWeight;
            ForegroundBrush = foregroundBrush;
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
        /// Gets the font family.
        /// </summary>
        public FontFamily FontFamily { get; }

        /// <summary>
        /// Gets the font size, in device independent pixels.
        /// </summary>
        public double? FontSize { get; }

        /// <summary>
        /// Gets the font style.
        /// </summary>
        public FontStyle? FontStyle{ get; }

        /// <summary>
        /// Gets the font weight.
        /// </summary>
        public FontWeight? FontWeight { get; }

        /// <summary>
        /// Gets the span's foreground brush.
        /// </summary>
        public IBrush ForegroundBrush { get; }
    }
}
