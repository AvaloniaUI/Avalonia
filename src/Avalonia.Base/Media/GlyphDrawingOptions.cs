using System;

namespace Avalonia.Media
{
    /// <summary>
    /// Options that influence how a single glyph is drawn, independent of variable-font
    /// axis configuration: which CPAL palette to use for color glyphs, and which bitmap
    /// strike size to prefer for bitmap-based glyphs.
    /// </summary>
    /// <remarks>
    /// All properties are optional. <c>null</c> means "use the font's default" (palette 0
    /// for color glyphs; no bitmap strike preference). Concerns specific to variable fonts
    /// (axis coordinates, named instances) live on <see cref="FontVariationSettings"/>.
    /// </remarks>
    public sealed record GlyphDrawingOptions
    {
        /// <summary>
        /// Singleton instance representing "use the font's defaults".
        /// </summary>
        public static GlyphDrawingOptions Default { get; } = new();

        private readonly int? _paletteIndex;
        private readonly int? _pixelSize;

        /// <summary>
        /// Gets the optional <c>CPAL</c> palette index used to resolve colors for
        /// COLR v0 / COLR v1 glyphs.
        /// </summary>
        /// <remarks>
        /// When <c>null</c>, the font's default palette (palette 0) is used.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Set to a negative value.</exception>
        public int? PaletteIndex
        {
            get => _paletteIndex;
            init
            {
                if (value is { } v && v < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), v, "PaletteIndex must be non-negative.");
                }

                _paletteIndex = value;
            }
        }

        /// <summary>
        /// Gets the optional pixel size used to select a bitmap strike from <c>sbix</c>,
        /// <c>CBDT</c> or <c>EBDT</c> tables.
        /// </summary>
        /// <remarks>
        /// When <c>null</c>, no bitmap strike is selected and the font's outline (or
        /// color-layer) representation is used instead.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Set to a value less than 1.</exception>
        public int? PixelSize
        {
            get => _pixelSize;
            init
            {
                if (value is { } v && v < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), v, "PixelSize must be at least 1.");
                }

                _pixelSize = value;
            }
        }
    }
}
