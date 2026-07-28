using System.Globalization;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Generic implementation of TextRunProperties
    /// </summary>
    public class GenericTextRunProperties : TextRunProperties
    {
        internal const double DefaultFontRenderingEmSize = 12;

        public GenericTextRunProperties(
            Typeface typeface,
            double fontRenderingEmSize = DefaultFontRenderingEmSize,
            TextDecorationCollection? textDecorations = null,
            IBrush? foregroundBrush = null,
            IBrush? backgroundBrush = null,
            BaselineAlignment baselineAlignment = BaselineAlignment.Baseline,
            CultureInfo? cultureInfo = null,
            FontFeatureCollection? fontFeatures = null)
        {
            Typeface = typeface;
            FontRenderingEmSize = fontRenderingEmSize;
            TextDecorations = textDecorations;
            ForegroundBrush = foregroundBrush;
            BackgroundBrush = backgroundBrush;
            BaselineAlignment = baselineAlignment;
            CultureInfo = cultureInfo;
            FontFeatures = fontFeatures;
        }

        /// <inheritdoc />
        public override Typeface Typeface { get; }

        /// <inheritdoc />
        public override double FontRenderingEmSize { get; }

        /// <inheritdoc />
        public override TextDecorationCollection? TextDecorations { get; }

        /// <inheritdoc />
        public override IBrush? ForegroundBrush { get; }

        /// <inheritdoc />
        public override IBrush? BackgroundBrush { get; }

        /// <inheritdoc />
        public override FontFeatureCollection? FontFeatures { get; }

        /// <inheritdoc />
        public override BaselineAlignment BaselineAlignment { get; }

        /// <inheritdoc />
        public override CultureInfo? CultureInfo { get; }
    }
}
