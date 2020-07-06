using System.Globalization;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Generic implementation of TextRunProperties
    /// </summary>
    public class GenericTextRunProperties : TextRunProperties
    {
        public GenericTextRunProperties(Typeface typeface, double fontRenderingEmSize = 12,
            TextDecorationCollection textDecorations = null, IBrush foregroundBrush = null, IBrush backgroundBrush = null,
            CultureInfo cultureInfo = null)
        {
            Typeface = typeface;
            FontRenderingEmSize = fontRenderingEmSize;
            TextDecorations = textDecorations;
            ForegroundBrush = foregroundBrush;
            BackgroundBrush = backgroundBrush;
            CultureInfo = cultureInfo;
        }

        /// <inheritdoc />
        public override Typeface Typeface { get; }

        /// <inheritdoc />
        public override double FontRenderingEmSize { get; }

        /// <inheritdoc />
        public override TextDecorationCollection TextDecorations { get; }

        /// <inheritdoc />
        public override IBrush ForegroundBrush { get; }

        /// <inheritdoc />
        public override IBrush BackgroundBrush { get; }

        /// <inheritdoc />
        public override CultureInfo CultureInfo { get; }
    }
}
