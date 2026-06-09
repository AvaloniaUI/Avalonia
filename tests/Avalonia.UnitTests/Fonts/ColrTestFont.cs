namespace Avalonia.UnitTests
{
    /// <summary>
    /// Helpers for COLR/CPAL color-glyph tests: grafts a hand-built COLR table (plus a minimal
    /// CPAL) onto a real outline font so the color-glyph pipeline can be driven end-to-end.
    /// </summary>
    public static class ColrTestFont
    {
        /// <summary>
        /// Grafts <paramref name="colr"/> and a minimal CPAL onto <paramref name="baseFont"/> (which
        /// supplies the outline glyphs, cmap, metrics, etc.). The caller chooses the base font so the
        /// helper works from any test assembly. Returns the editable font; call
        /// <see cref="SyntheticFont.TryCreateGlyphTypeface"/> to realize it.
        /// </summary>
        public static SyntheticFont Graft(SyntheticFont baseFont, byte[] colr)
            => baseFont
                .Replace("COLR", colr)
                .Replace("CPAL", MinimalCpal());

        /// <summary>A minimal valid CPAL v0 table: one palette of one (opaque black) color.</summary>
        public static byte[] MinimalCpal()
        {
            var cpal = new BigEndianBuffer();

            cpal.UInt16(0);   // version
            cpal.UInt16(1);   // numPaletteEntries
            cpal.UInt16(1);   // numPalettes
            cpal.UInt16(1);   // numColorRecords
            var colorRecordsOffsetPos = cpal.ReserveOffset32(); // colorRecordsArrayOffset @8

            cpal.UInt16(0);   // colorRecordIndices[0]

            cpal.PatchUInt32(colorRecordsOffsetPos, (uint)cpal.Position);
            cpal.UInt8(0).UInt8(0).UInt8(0).UInt8(0xFF); // one BGRA color record (opaque black)

            return cpal.ToArray();
        }
    }
}
