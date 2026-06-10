namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// The identity of one cached glyph payload in a <see cref="GlyphCache"/>: which glyph, which
    /// <em>requested</em> representation, and — for colour drawings — which CPAL palette it was
    /// resolved with. One map keyed by this struct holds every representation, so a colour base
    /// glyph, its palette variants and its own fallback outline coexist as distinct entries.
    /// </summary>
    /// <remarks>
    /// <see cref="Kind"/> is the representation a caller asked for, not the payload's final shape:
    /// whether an outline turns out to be a composite is only discovered during the build, so
    /// composites are requested — and keyed — as <see cref="GlyphPayloadKind.Outline"/>
    /// (<see cref="GlyphPayloadKind.CompositeOutline"/> never appears in a key; the constructor is
    /// private and the factories cannot produce it). <see cref="PaletteIndex"/> is expected to be
    /// normalized (an index the font defines, or 0) before keying, so undefined palettes share the
    /// default palette's entry; it is 0 for palette-independent representations. A bitmap-strike
    /// representation (<see cref="GlyphPayloadKind.Bitmap"/>, future) adds its pixel size here as a
    /// further field rather than as another map.
    /// </remarks>
    internal readonly record struct GlyphCacheKey
    {
        private GlyphCacheKey(ushort glyphId, GlyphPayloadKind kind, ushort paletteIndex)
        {
            GlyphId = glyphId;
            Kind = kind;
            PaletteIndex = paletteIndex;
        }

        /// <summary>The glyph id.</summary>
        public ushort GlyphId { get; }

        /// <summary>The requested representation (never <see cref="GlyphPayloadKind.CompositeOutline"/>).</summary>
        public GlyphPayloadKind Kind { get; }

        /// <summary>The normalized CPAL palette for colour drawings; 0 otherwise.</summary>
        public ushort PaletteIndex { get; }

        /// <summary>The key of a glyph's outline payload (composites are keyed as outlines).</summary>
        public static GlyphCacheKey Outline(ushort glyph)
            => new(glyph, GlyphPayloadKind.Outline, 0);

        /// <summary>The key of a glyph's colour drawing resolved with <paramref name="palette"/>.</summary>
        public static GlyphCacheKey Color(ushort glyph, ushort palette)
            => new(glyph, GlyphPayloadKind.ColorDrawing, palette);
    }
}
