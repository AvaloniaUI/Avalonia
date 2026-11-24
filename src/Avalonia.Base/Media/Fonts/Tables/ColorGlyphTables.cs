namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// The OpenType tables a colour font face draws its glyphs from: COLR layers, sbix or CBDT
    /// bitmaps, or SVG documents. A face with any of them is a colour font face.
    /// </summary>
    internal static class ColorGlyphTables
    {
        internal static OpenTypeTag Colr { get; } = OpenTypeTag.Parse("COLR");

        internal static OpenTypeTag Sbix { get; } = OpenTypeTag.Parse("sbix");

        internal static OpenTypeTag Cbdt { get; } = OpenTypeTag.Parse("CBDT");

        internal static OpenTypeTag Svg { get; } = OpenTypeTag.Parse("SVG ");

        internal static OpenTypeTag[] Tags { get; } = { Colr, Sbix, Cbdt, Svg };

        /// <summary>
        /// Gets whether <paramref name="tag"/> names a colour glyph table.
        /// </summary>
        internal static bool Contains(OpenTypeTag tag) => tag == Colr || tag == Sbix || tag == Cbdt || tag == Svg;
    }
}
