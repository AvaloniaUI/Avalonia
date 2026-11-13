namespace Avalonia.Media
{
    /// <summary>
    /// A size variant of a math glyph from the font's MATH table.
    /// </summary>
    /// <param name="Glyph">The glyph id of the variant.</param>
    /// <param name="Advance">
    /// The size of the variant in the direction it grows, in design units: its advance width for
    /// <see cref="MathDirection.Horizontal"/> and its advance height for <see cref="MathDirection.Vertical"/>.
    /// </param>
    public readonly record struct MathGlyphVariant(ushort Glyph, int Advance);
}
