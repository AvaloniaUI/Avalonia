using Avalonia.Media;

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
            => Graft(baseFont, colr, MinimalCpal());

        /// <summary>
        /// Grafts <paramref name="colr"/> and the given <paramref name="cpal"/> onto
        /// <paramref name="baseFont"/> — for tests that need specific palettes (see <see cref="Cpal"/>).
        /// </summary>
        public static SyntheticFont Graft(SyntheticFont baseFont, byte[] colr, byte[] cpal)
            => baseFont
                .Replace("COLR", colr)
                .Replace("CPAL", cpal);

        /// <summary>A minimal valid CPAL v0 table: one palette of one (opaque black) color.</summary>
        public static byte[] MinimalCpal() => Cpal(new[] { Colors.Black });

        /// <summary>
        /// A CPAL v0 table with one palette per argument. Every palette must have the same number of
        /// entries (a CPAL invariant); palette <c>i</c>'s colors are the <c>i</c>-th array.
        /// </summary>
        public static byte[] Cpal(params Color[][] palettes)
        {
            var entryCount = palettes[0].Length;
            var cpal = new BigEndianBuffer();

            cpal.UInt16(0);                                       // version
            cpal.UInt16((ushort)entryCount);                      // numPaletteEntries
            cpal.UInt16((ushort)palettes.Length);                 // numPalettes
            cpal.UInt16((ushort)(entryCount * palettes.Length));  // numColorRecords
            var colorRecordsOffsetPos = cpal.ReserveOffset32();   // colorRecordsArrayOffset @8

            for (var i = 0; i < palettes.Length; i++)
            {
                cpal.UInt16((ushort)(i * entryCount));            // colorRecordIndices[i]
            }

            cpal.PatchUInt32(colorRecordsOffsetPos, (uint)cpal.Position);

            foreach (var palette in palettes)
            {
                foreach (var color in palette)
                {
                    cpal.UInt8(color.B).UInt8(color.G).UInt8(color.R).UInt8(color.A); // BGRA records
                }
            }

            return cpal.ToArray();
        }
    }
}
