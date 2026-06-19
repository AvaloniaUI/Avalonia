namespace Avalonia.Media.Fonts.Tables.Metrics
{
    /// <summary>
    /// Represents a single horizontal metric record from the 'hmtx' table.
    /// </summary>
    internal readonly record struct HorizontalGlyphMetric
    {
        /// <summary>
        /// The advance width of the glyph.
        /// </summary>
        public ushort AdvanceWidth { get; }

        /// <summary>
        /// The left side bearing of the glyph.
        /// </summary>
        public short LeftSideBearing { get; }

        public HorizontalGlyphMetric(ushort advanceWidth, short leftSideBearing)
        {
            AdvanceWidth = advanceWidth;
            LeftSideBearing = leftSideBearing;
        }

        public override string ToString() => $"Advance={AdvanceWidth}, LSB={LeftSideBearing}";
    }
}
