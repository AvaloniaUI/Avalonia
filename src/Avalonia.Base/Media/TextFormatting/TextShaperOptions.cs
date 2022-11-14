using System.Globalization;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Options to customize text shaping.
    /// </summary>
    public readonly struct TextShaperOptions
    {
        public TextShaperOptions(
            IGlyphTypeface typeface, 
            double fontRenderingEmSize = 12, 
            sbyte bidiLevel = 0, 
            CultureInfo? culture = null, 
            double incrementalTabWidth = 0,
            double letterSpacing = 0)
        {
            Typeface = typeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
            Culture = culture;
            IncrementalTabWidth = incrementalTabWidth;
            LetterSpacing = letterSpacing;
        }

        /// <summary>
        /// Get the typeface.
        /// </summary>
        public IGlyphTypeface Typeface { get; }
        /// <summary>
        /// Get the font rendering em size.
        /// </summary>
        public double FontRenderingEmSize { get; }

        /// <summary>
        /// Get the bidi level of the text.
        /// </summary>
        public sbyte BidiLevel { get; }

        /// <summary>
        /// Get the culture.
        /// </summary>
        public CultureInfo? Culture { get; }

        /// <summary>
        /// Get the incremental tab width.
        /// </summary>
        public double IncrementalTabWidth { get; }

        /// <summary>
        /// Get the letter spacing.
        /// </summary>
        public double LetterSpacing { get; }

    }
}
