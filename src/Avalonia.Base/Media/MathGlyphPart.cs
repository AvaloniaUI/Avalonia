namespace Avalonia.Media
{
    /// <summary>
    /// A part of a math glyph assembly, in design units.
    /// </summary>
    /// <param name="Glyph">The glyph id of the part.</param>
    /// <param name="StartConnectorLength">
    /// The length of the connector at the start of the part, the bottom for a vertical assembly and
    /// the left for a horizontal one, that may overlap the previous part.
    /// </param>
    /// <param name="EndConnectorLength">The length of the connector at the end of the part.</param>
    /// <param name="FullAdvance">The full size of the part in the direction of the assembly.</param>
    /// <param name="IsExtender">
    /// Whether the part may be repeated, or left out, to reach the size the layout needs.
    /// </param>
    public readonly record struct MathGlyphPart(
        ushort Glyph,
        int StartConnectorLength,
        int EndConnectorLength,
        int FullAdvance,
        bool IsExtender);
}
