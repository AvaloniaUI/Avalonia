using System;
using Avalonia.Media;

namespace Avalonia.Documents
{
    /// <summary>
    /// Base class for inline text elements such as <see cref="Span"/> and <see cref="Run"/>/
    /// </summary>
    public abstract class Inline : TextElement
    {
        /// <summary>
        /// Parses a string to create an <see cref="Inline"/>.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The inline.</returns>
        public Inline Parse(string s) => new Run(s);

        /// <summary>
        /// Builds the <see cref="FormattedText"/> for the inline text element.
        /// </summary>
        /// <param name="builder">The formatted text builder.</param>
        public abstract void BuildFormattedText(FormattedTextBuilder builder);

        /// <summary>
        /// Creates a <see cref="FormattedTextStyleSpan"/> for the specified span, using the
        /// current text element's styling properties.
        /// </summary>
        /// <param name="startIndex">The start of the span.</param>
        /// <param name="length">The length of the span.</param>
        /// <returns>
        /// A <see cref="FormattedTextStyleSpan"/> or null if no relevant styling properties are
        /// set on the <see cref="Inline"/>.
        /// </returns>
        protected FormattedTextStyleSpan CreateStyleSpan(int startIndex, int length)
        {
            var fontFamily = IsSet(FontFamilyProperty) ? FontFamily : null;
            var fontSize = IsSet(FontSizeProperty) ? (double?)FontSize : null;
            var fontStyle = IsSet(FontStyleProperty) ? (FontStyle?)FontStyle : null;
            var fontWeight = IsSet(FontWeightProperty) ? (FontWeight?)FontWeight : null;
            var brush = IsSet(ForegroundProperty) ? Foreground : null;

            if (fontFamily != null ||
                fontSize != null ||
                fontStyle != null ||
                fontWeight != null ||
                brush != null)
            {
                return new FormattedTextStyleSpan(
                    startIndex,
                    length,
                    fontFamily: fontFamily,
                    fontSize: fontSize,
                    fontStyle: fontStyle,
                    fontWeight: fontWeight,
                    foregroundBrush: brush);
            }
            else
            {
                return null;
            }
        }
    }
}
