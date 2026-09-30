namespace Avalonia.Media;

public readonly record struct GlyphMetrics
{
    /// <summary>
    /// Distance from the x-origin to the leftmost outline point.
    /// </summary>
    public int XBearing { get; init; }

    /// <summary>
    /// Distance from the topmost outline point to the y-origin.
    /// </summary>
    public int YBearing { get; init; }

    /// <summary>
    /// Width of the glyph's outline bounding box.
    /// </summary>
    public ushort Width { get; init; }

    /// <summary>
    /// Height of the glyph's outline bounding box.
    /// </summary>
    public ushort Height { get; init; }

    /// <summary>
    /// Horizontal advance width (distance to the next glyph's origin).
    /// </summary>
    public ushort AdvanceWidth { get; init; }

    /// <summary>
    /// Vertical advance height (distance to the next glyph's origin in vertical layout).
    /// </summary>
    public ushort AdvanceHeight { get; init; }

    /// <summary>
    /// Horizontal offset from the glyph's origin to the leftmost outline point (used for bitmap glyphs).
    /// </summary>
    public ushort XOffset { get; init; }

    /// <summary>
    /// Vertical offset from the glyph's origin to the topmost outline point (used for bitmap glyphs).
    /// </summary>
    public ushort YOffset { get; init; }

    /// <summary>
    /// X coordinate of the vertical origin (used for vertical layout).
    /// </summary>
    public ushort VerticalOriginX { get; init; }

    /// <summary>
    /// Y coordinate of the vertical origin (used for vertical layout).
    /// </summary>
    public ushort VerticalOriginY { get; init; }
}
