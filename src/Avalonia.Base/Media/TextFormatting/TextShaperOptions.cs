using System.Globalization;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Options to customize text shaping.
    /// </summary>
    public readonly struct TextShaperOptions
    {
        public TextShaperOptions(
            GlyphTypeface typeface, 
            double fontRenderingEmSize = 12, 
            sbyte bidiLevel = 0, 
            CultureInfo? culture = null, 
            double incrementalTabWidth = 0)
        {
            Typeface = typeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidLevel = bidiLevel;
            Culture = culture;
            IncrementalTabWidth = incrementalTabWidth;
        }

        /// <summary>
        /// Get the typeface.
        /// </summary>
        public GlyphTypeface Typeface { get; }
        /// <summary>
        /// Get the font rendering em size.
        /// </summary>
        public double FontRenderingEmSize { get; }

        /// <summary>
        /// Get the bidi level of the text.
        /// </summary>
        public sbyte BidLevel { get; }

        /// <summary>
        /// Get the culture.
        /// </summary>
        public CultureInfo? Culture { get; }

        /// <summary>
        /// Get the incremental tab width.
        /// </summary>
        public double IncrementalTabWidth { get; }

    }
}
