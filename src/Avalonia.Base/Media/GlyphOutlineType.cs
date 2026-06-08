namespace Avalonia.Media
{
    /// <summary>
    /// Identifies the vector-outline technology a <see cref="GlyphTypeface"/> uses for its glyphs.
    /// </summary>
    public enum GlyphOutlineType
    {
        /// <summary>
        /// The font has no vector outlines this typeface can produce — e.g. a bitmap-strike or
        /// SVG-only font. <see cref="GlyphTypeface.GetGlyphOutline(ushort)"/> returns <c>null</c>.
        /// </summary>
        None,

        /// <summary>TrueType quadratic outlines (the <c>glyf</c> table).</summary>
        TrueType,

        /// <summary>
        /// PostScript / Type 2 cubic outlines (the <c>CFF </c> table — the <c>.otf</c> flavour).
        /// </summary>
        Cff,

        /// <summary>Variable PostScript outlines (the <c>CFF2</c> table).</summary>
        Cff2
    }
}
