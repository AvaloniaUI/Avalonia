namespace Avalonia.Media
{
    /// <summary>
    /// Represents a glyph that knows how to draw itself into a <see cref="DrawingContext"/>.
    /// </summary>
    /// <remarks>
    /// Implementations are produced by the per-format renderers (color layers from
    /// COLR/CPAL, bitmap strikes from sbix/CBDT, etc.) and shielded behind this contract
    /// so callers can render any color glyph without caring which font-table format
    /// produced it. For plain outline glyphs, use <c>GlyphTypeface.GetGlyphOutline</c>
    /// instead — outlines are returned as <see cref="Geometry"/> rather than via this
    /// interface.
    /// </remarks>
    public interface IGlyphDrawing
    {
        /// <summary>
        /// Gets the format this drawing was produced from. Callers can use this to
        /// branch on rendering behaviour without downcasting.
        /// </summary>
        GlyphDrawingType Type { get; }

        /// <summary>
        /// Gets the axis-aligned bounding rectangle of the drawing, in font design units
        /// (the font's units-per-em space, no em-size scaling) with the Y axis pointing down.
        /// </summary>
        /// <remarks>
        /// The rectangle is relative to the drawing's local origin. To get bounds at a
        /// specific paint location, translate by the origin passed to <see cref="Draw"/>.
        /// Implementations are expected to compute this once (e.g. from the font's clip
        /// box or layer extents) and cache it.
        /// </remarks>
        Rect Bounds { get; }

        /// <summary>
        /// Draws the glyph into <paramref name="context"/> at <paramref name="origin"/>.
        /// </summary>
        /// <remarks>
        /// The glyph is drawn in font design units, one drawing unit per font unit. To draw it at
        /// an em size, push a scale of <c>emSize / DesignEmHeight</c> onto
        /// <paramref name="context"/> first.
        /// </remarks>
        /// <param name="context">The drawing context to render to.</param>
        /// <param name="origin">
        /// The point, in the coordinate space of <paramref name="context"/> (Y-down), at which
        /// the glyph's local origin should land. For text rendering this is typically the pen position on the baseline.
        /// Implementations apply the Y-flip from font-space (Y-up) internally, so
        /// callers don't need to flip <paramref name="origin"/> themselves.
        /// </param>
        void Draw(DrawingContext context, Point origin);
    }
}
