namespace Avalonia.Media
{
    /// <summary>
    /// A glyph's control-point bounding box in font design units, as stored in the
    /// <c>glyf</c> header. Used by the batch bounds path
    /// (<see cref="GlyphTypeface.TryGetGlyphBounds"/>) where only the ink extent is
    /// needed and advances are already known by the caller.
    /// </summary>
    internal readonly record struct GlyphBounds(short XMin, short YMin, short XMax, short YMax)
    {
        /// <summary>Width of the bounding box (<see cref="XMax"/> − <see cref="XMin"/>).</summary>
        public int Width => XMax - XMin;

        /// <summary>Height of the bounding box (<see cref="YMax"/> − <see cref="YMin"/>).</summary>
        public int Height => YMax - YMin;
    }
}
