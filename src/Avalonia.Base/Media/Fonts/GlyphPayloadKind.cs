namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Discriminates the shape of a <see cref="GlyphCacheEntry.Geometry"/> so the cache can treat the
    /// different glyph representations uniformly while consumers down-cast to the concrete type.
    /// </summary>
    internal enum GlyphPayloadKind : byte
    {
        /// <summary>A simple glyph: one <c>IGeometryImpl</c> (CFF / CFF2, or a non-composite glyf glyph).</summary>
        Outline,

        /// <summary>
        /// A glyf composite glyph: one (flattened) <c>IGeometryImpl</c>, with
        /// <see cref="GlyphCacheEntry.Dependencies"/> listing its component glyph IDs. The payload is
        /// self-contained, so components are kept warm by recency propagation rather than pinning.
        /// </summary>
        CompositeOutline,

        /// <summary>
        /// A COLR color glyph (Part 14): a drawing recording whose payload <em>references</em> its layer
        /// glyphs' outlines. Such referencing payloads pin their <see cref="GlyphCacheEntry.Dependencies"/>.
        /// </summary>
        ColorDrawing,

        /// <summary>A bitmap-strike glyph (<c>sbix</c> / <c>CBDT</c>, future): a pixel payload.</summary>
        Bitmap
    }
}
