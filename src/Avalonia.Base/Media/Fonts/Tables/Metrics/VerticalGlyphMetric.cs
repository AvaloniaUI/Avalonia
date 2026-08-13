namespace Avalonia.Media.Fonts.Tables.Metrics
{
    /// <summary>
    /// Represents a single vertical metric record from the 'vmtx' table.
    /// </summary>
    internal readonly record struct VerticalGlyphMetric
    {
        public VerticalGlyphMetric(ushort advanceHeight, short topSideBearing)
        {
            AdvanceHeight = advanceHeight;
            TopSideBearing = topSideBearing;
        }

        /// <summary>
        /// The advance height of the glyph.
        /// </summary>
        public ushort AdvanceHeight { get; }

        /// <summary>
        /// The top side bearing of the glyph.
        /// </summary>
        public short TopSideBearing { get; }
    }
}
