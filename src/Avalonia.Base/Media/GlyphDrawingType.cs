namespace Avalonia.Media
{
    /// <summary>
    /// Specifies the format used to render a glyph, such as outline, color layers, SVG, or bitmap.
    /// </summary>
    /// <remarks>Use this enumeration to determine or specify how a glyph should be drawn, depending on the
    /// font's supported formats. The value corresponds to the glyph's representation in the font file, which may affect
    /// rendering capabilities and visual appearance.</remarks>
    public enum GlyphDrawingType
    {
        Outline,     // glyf / CFF / CFF2
        ColorLayers, // COLR/CPAL
        Svg,         // SVG table
        Bitmap       // sbix / CBDT / EBDT
    }
}
