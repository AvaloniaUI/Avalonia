using System;
using Avalonia.Media;

namespace Avalonia.Documents
{
    /// <summary>
    /// Base class for inline text elements such as <see cref="Span"/> and <see cref="Run"/>/
    /// </summary>
    public abstract class Inline : TextElement
    {
        public Inline Parse(string s) => new Run(s);

        public abstract void BuildFormattedText(FormattedTextBuilder builder);

        protected FormattedTextStyleSpan GetStyleSpan(int startIndex, int length)
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
