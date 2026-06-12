using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// Characterization tests for the cmap / name / post robustness of the GlyphTypeface parsers,
    /// using the <see cref="SyntheticFont"/> / <see cref="BigEndianBuffer"/> harness. A present-but-
    /// malformed cosmetic table must degrade to the same fallback as an absent one rather than denying
    /// the whole font, and cmap parsing must not overflow or mis-select a subtable.
    /// </summary>
    public class MalformedCmapNamePostTests
    {
        // ── present-but-malformed cosmetic tables must not deny the font ──
        //
        // A *missing* 'post' / 'name' table is handled gracefully (see the two "Missing_*" tests
        // below), and so is a *present-but-malformed* (truncated) one: the loader isolates the
        // over-reading parse and degrades to the same fallback as an absent table.

        [Fact]
        public void Truncated_Post_Table_Does_Not_Deny_The_Font()
        {
            // Keep only the 4-byte version field; reading the rest of the header over-runs.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).Truncate("post", 4);

            var typeface = font.TryCreateGlyphTypeface();

            // A malformed 'post' (underline / italic-angle / fixed-pitch hints only) degrades to
            // defaults; the rest of the font — including the intact 'name' — still loads.
            Assert.NotNull(typeface);
            Assert.Equal("Inter", typeface!.FamilyName);
        }

        [Fact]
        public void Truncated_Name_Table_Falls_Back_To_Unknown_Family()
        {
            // Keep only format + count; reading stringOffset and the record array over-runs.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).Truncate("name", 4);

            var typeface = font.TryCreateGlyphTypeface();

            // A malformed 'name' degrades to no name table, so the family name falls back to "unknown"
            // instead of denying a renderable font.
            Assert.NotNull(typeface);
            Assert.Equal("unknown", typeface!.FamilyName);
        }

        [Fact]
        public void Missing_Post_Table_Still_Loads_The_Font()
        {
            // Documents the already-correct path: an *absent* cosmetic table is tolerated.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).Remove("post");

            var typeface = font.TryCreateGlyphTypeface();

            Assert.NotNull(typeface);
            Assert.Equal("Inter", typeface!.FamilyName);
        }

        [Fact]
        public void Missing_Name_Table_Falls_Back_To_Unknown_Family()
        {
            // Documents the already-correct path: an *absent* name table yields a usable typeface
            // with a fallback family name.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).Remove("name");

            var typeface = font.TryCreateGlyphTypeface();

            Assert.NotNull(typeface);
            Assert.Equal("unknown", typeface!.FamilyName);
        }

        // ── cmap format 12 nGroups*12 overflow denies the whole font ──

        [Fact]
        public void Cmap_Format12_NGroups_Overflow_Does_Not_Deny_The_Font()
        {
            // numGroups = 0x20000000 would make `numGroups * 12` overflow int32 to a negative slice
            // length and throw out of the (throwing) CmapTable.Load.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .Replace("cmap", BuildCmapWithOverflowingFormat12(numGroups: 0x20000000));

            var typeface = font.TryCreateGlyphTypeface();

            // The group count is now computed in long and clamped to what the table holds, so the bad
            // subtable yields empty coverage rather than throwing — the font still loads.
            Assert.NotNull(typeface);
        }

        // ── cmap Format-4 subtable selection prefers Unicode over Symbol ──

        [Theory]
        [InlineData(true)]   // Symbol subtable listed first
        [InlineData(false)]  // Unicode subtable listed first
        public void Format4_Subtable_Selection_Prefers_Unicode_Over_Symbol(bool symbolFirst)
        {
            // Two Windows-platform Format-4 subtables: a Symbol (encoding 0) one that maps only the
            // PUA codepoint 0xF041, and a Unicode-BMP (encoding 1) one that maps 'A'. The selection
            // now scores the Symbol encoding worse than Unicode, so the Unicode subtable wins
            // regardless of directory order.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .Replace("cmap", BuildDualFormat4Cmap(symbolFirst));

            var typeface = font.TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            // ASCII 'A' now resolves in BOTH orderings — encoding, not directory order, decides.
            Assert.True(typeface!.CharacterToGlyphMap.ContainsGlyph('A'));
        }

        private static byte[] BuildCmapWithOverflowingFormat12(uint numGroups)
        {
            // Format-12 subtable: format(2) reserved(2) length(4) language(4) numGroups(4) groups[…].
            // length is honest about the 16-byte buffer, so the length-slice succeeds and the
            // overflow surfaces at the group-array slice (the exact path under test).
            var subtable = new BigEndianBuffer()
                .UInt16(12)        // format
                .UInt16(0)         // reserved
                .UInt32(16)        // length (header only)
                .UInt32(0)         // language
                .UInt32(numGroups) // numGroups
                .ToArray();

            // cmap header: version(2) numTables(2), then one EncodingRecord: platform(2) encoding(2) offset(4).
            var cmap = new BigEndianBuffer();
            cmap.UInt16(0);   // version
            cmap.UInt16(1);   // numTables
            cmap.UInt16(3);   // platformID = Windows
            cmap.UInt16(10);  // encodingID = UCS-4 (any value works; format 12 is selected regardless)
            var offsetPos = cmap.ReserveOffset32();
            cmap.PatchUInt32(offsetPos, (uint)cmap.Position);
            cmap.Bytes(subtable);

            return cmap.ToArray();
        }

        /// <summary>
        /// Builds a cmap with two Windows-platform Format-4 subtables — a Symbol (encoding 0) one
        /// mapping the PUA codepoint 0xF041 and a Unicode-BMP (encoding 1) one mapping 'A' — ordered
        /// per <paramref name="symbolFirst"/>.
        /// </summary>
        private static byte[] BuildDualFormat4Cmap(bool symbolFirst)
        {
            var symbol = BuildSingleCharFormat4(charCode: 0xF041, glyph: 7);
            var unicode = BuildSingleCharFormat4(charCode: 'A', glyph: 5);

            var firstSub = symbolFirst ? symbol : unicode;
            var firstEncoding = symbolFirst ? 0 : 1;   // Symbol = 0, UnicodeBMP = 1
            var secondSub = symbolFirst ? unicode : symbol;
            var secondEncoding = symbolFirst ? 1 : 0;

            var cmap = new BigEndianBuffer();
            cmap.UInt16(0);   // version
            cmap.UInt16(2);   // numTables

            // Two 8-byte EncodingRecords follow the 4-byte header, so the subtables start at offset 20.
            const int subtablesStart = 4 + 2 * 8;
            cmap.UInt16(3); cmap.UInt16(firstEncoding); cmap.UInt32(subtablesStart);
            cmap.UInt16(3); cmap.UInt16(secondEncoding); cmap.UInt32((uint)(subtablesStart + firstSub.Length));
            cmap.Bytes(firstSub);
            cmap.Bytes(secondSub);

            return cmap.ToArray();
        }

        /// <summary>Builds a minimal Format-4 cmap subtable mapping a single <paramref name="charCode"/> to <paramref name="glyph"/>.</summary>
        private static byte[] BuildSingleCharFormat4(int charCode, int glyph)
        {
            // Two segments: [charCode, charCode] and the mandatory terminal [0xFFFF, 0xFFFF].
            // No glyphIdArray — the glyph comes from idDelta (idRangeOffset = 0). Total length 32.
            return new BigEndianBuffer()
                .UInt16(4)        // format
                .UInt16(32)       // length
                .UInt16(0)        // language
                .UInt16(4)        // segCountX2 (segCount = 2)
                .UInt16(4)        // searchRange
                .UInt16(1)        // entrySelector
                .UInt16(0)        // rangeShift
                .UInt16(charCode).UInt16(0xFFFF)                 // endCode[2]
                .UInt16(0)                                       // reservedPad
                .UInt16(charCode).UInt16(0xFFFF)                 // startCode[2]
                .UInt16((glyph - charCode) & 0xFFFF).UInt16(1)   // idDelta[2]
                .UInt16(0).UInt16(0)                             // idRangeOffset[2]
                .ToArray();
        }
    }
}
