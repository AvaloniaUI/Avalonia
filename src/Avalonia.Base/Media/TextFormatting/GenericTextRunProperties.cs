using System;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Generic implementation of TextRunProperties
    /// </summary>
    public class GenericTextRunProperties : TextRunProperties
    {
        private const double DefaultFontRenderingEmSize = 12;

        // TODO12: Remove in 12.0.0 and make fontFeatures parameter in main ctor optional
        public GenericTextRunProperties(Typeface typeface, double fontRenderingEmSize = DefaultFontRenderingEmSize,
            TextDecorationCollection? textDecorations = null, IBrush? foregroundBrush = null,
            IBrush? backgroundBrush = null, BaselineAlignment baselineAlignment = BaselineAlignment.Baseline,
            CultureInfo? cultureInfo = null) : 
            this(typeface, null, fontRenderingEmSize, textDecorations, foregroundBrush,
            backgroundBrush, baselineAlignment, cultureInfo)
        {
        }
        
        // TODO12:Change signature in 12.0.0
        public GenericTextRunProperties(
            Typeface typeface, 
            FontFeatureCollection? fontFeatures, 
            double fontRenderingEmSize = DefaultFontRenderingEmSize,
            TextDecorationCollection? textDecorations = null,
            IBrush? foregroundBrush = null,
            IBrush? backgroundBrush = null,
            BaselineAlignment baselineAlignment = BaselineAlignment.Baseline,
            CultureInfo? cultureInfo = null)
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
