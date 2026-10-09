using System;
using System.Buffers.Binary;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// Characterization tests for the ItemVariationStore / HVAR offset validation and the hmtx
    /// metric-count clamp, using the <see cref="SyntheticFont"/> / <see cref="BigEndianBuffer"/>
    /// harness. A hostile offset must be rejected without throwing, a corrupt HVAR must degrade to
    /// "no advance variation", and a degenerate hmtx count must not throw at layout time.
    /// </summary>
    public class MalformedVariationStoreMetricsTests
    {
        // ── (int)-cast uint32 offsets slip additive length guards ──

        [Fact]
        public void ItemVariationStore_TryLoad_Returns_False_On_Hostile_RegionListOffset()
        {
            // An ItemVariationStore header: format(uint16), regionListOffset(uint32), ivdCount(uint16).
            // regionListOffset = 0xFFFFFFFF previously cast to int -1, slipped the additive length
            // guard, and threw on span.Slice(-1).
            var ivs = new BigEndianBuffer()
                .UInt16(1)            // format = 1 (the only supported format)
                .UInt32(0xFFFFFFFF)   // regionListOffset (hostile)
                .UInt16(0)            // ivdCount = 0
                .ToArray();

            var loaded = true;
            var exception = Record.Exception(() => loaded = ItemVariationStore.TryLoad(ivs, expectedAxisCount: 2, out _));

            // Now validated as unsigned / range-safe: TryLoad returns false instead of throwing.
            Assert.Null(exception);
            Assert.False(loaded);
        }

        [Fact]
        public void ItemVariationStore_TryGetDelta_Does_Not_Throw_On_Overflowing_Row_Offset()
        {
            // A minimal but valid store with one region and one ItemVariationData subtable whose
            // itemCount is huge. innerIndex * rowBytes overflowed int to a negative rowStart that
            // slipped the `rowStart + rowBytes > length` guard and then threw on the span slice.
            // The row math is now computed in long.
            const int regionListOffset = 8 + 4; // header(8) + one ivd offset(4)

            var ivs = new BigEndianBuffer();
            ivs.UInt16(1);                    // format
            ivs.UInt32(regionListOffset);     // regionListOffset
            ivs.UInt16(1);                    // ivdCount
            ivs.UInt32(regionListOffset + 4 + 1 * 2 * 6); // ivd[0] offset: after the region list

            // VariationRegionList: axisCount(2), regionCount(2), then regionCount*axisCount*6.
            ivs.UInt16(2);                    // axisCount (matches expectedAxisCount)
            ivs.UInt16(1);                    // regionCount
            // One region, two axes: start/peak/end F2Dot14 each.
            ivs.F2Dot14(0).F2Dot14(1).F2Dot14(1);
            ivs.F2Dot14(0).F2Dot14(0).F2Dot14(0);

            // ItemVariationData: itemCount(2), wordDeltaCount(2), regionIndexCount(2), regionIndexes[].
            ivs.UInt16(0xFFFF);               // itemCount (huge — lets innerIndex be large)
            ivs.UInt16(1);                    // wordDeltaCount
            ivs.UInt16(1);                    // regionIndexCount
            ivs.UInt16(0);                    // regionIndexes[0]
            // No actual delta rows — the bounds guard must reject before reading them.

            Assert.True(ItemVariationStore.TryLoad(ivs.ToArray(), expectedAxisCount: 2, out var store));

            var active = new[] { 1f, 0f };
            var exception = Record.Exception(
                () => store!.TryGetDelta(outerIndex: 0, innerIndex: 0xFFFE, active, out _));

            // No throw; the out-of-range row is rejected by the (now long) bounds check.
            Assert.Null(exception);
        }

        [Fact]
        public void DeltaSetIndexMap_Format1_Rejects_Hostile_MapCount()
        {
            // Format 1 carries a uint32 mapCount. 0xFFFFFFFF cast straight to int is negative; the
            // `entriesStart + (long)mapCount * entrySize` guard then computed a negative product and
            // failed open. The raw count is now range-checked unsigned.
            var map = new BigEndianBuffer()
                .UInt8(1)            // format = 1
                .UInt8(0)            // entryFormat (1 byte/entry, 1-bit inner index)
                .UInt32(0xFFFFFFFF)  // mapCount (hostile)
                .ToArray();

            var loaded = true;
            var exception = Record.Exception(() => loaded = DeltaSetIndexMap.TryLoad(map, out _));

            Assert.Null(exception);
            Assert.False(loaded);
        }

        [Fact]
        public void Corrupt_Hvar_AdvanceMapOffset_Does_Not_Deny_The_Font()
        {
            // HVAR header: ..., advanceWidthMappingOffset is the uint32 at offset 8. A high-bit
            // offset cast to a negative int slipped the slice and threw out of the constructor.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable);

            font.PatchUInt32("HVAR", 8, 0xFFFFFFFF);

            var typeface = font.TryCreateGlyphTypeface();

            // The offset is now validated unsigned, so a corrupt advance map degrades to "no HVAR"
            // rather than denying the font.
            Assert.NotNull(typeface);
        }

        [Fact]
        public void Corrupt_Hvar_VariationStore_Does_Not_Deny_The_Font()
        {
            // Drive the same input through the real load path: corrupt the regionListOffset *inside*
            // InterVariable's HVAR ItemVariationStore. The GlyphTypeface constructor loads HVAR eagerly.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable);

            var hvar = font.GetTable("HVAR");
            // HVAR header: majorVersion(2), minorVersion(2), itemVariationStoreOffset(4), ...
            var ivsOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(hvar.AsSpan(4));

            // regionListOffset is the uint32 at offset +2 within the ItemVariationStore.
            font.PatchUInt32("HVAR", ivsOffset + 2, 0xFFFFFFFF);

            var typeface = font.TryCreateGlyphTypeface();

            // A malformed HVAR now degrades to "no advance variation" instead of denying the font.
            Assert.NotNull(typeface);
        }

        // ── hmtx accessors throw at *layout* time on a degenerate metric count ──
        //
        // Unlike the load-time cases, these surface at runtime: the GlyphTypeface constructor stores
        // the counts without touching the per-glyph arrays, so the throw only fires when text layout
        // later queries an advance.

        [Fact]
        public void Zero_NumberOfHMetrics_Does_Not_Throw_From_Advance_Lookup()
        {
            // hhea.numberOfHMetrics is the last uint16, at offset 34. Zero previously made the "repeat
            // last advance" math compute (numberOfHMetrics - 1) * 4 = -4 → a negative slice/seek.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).PatchUInt16("hhea", 34, 0);

            var typeface = font.TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            // The accessor now treats a zero metric count as "no horizontal metrics": it returns false
            // rather than throwing at layout time.
            var advance = true;
            var exception = Record.Exception(() => advance = typeface!.TryGetHorizontalGlyphAdvance(3, out _));
            Assert.Null(exception);
            Assert.False(advance);
        }

        [Fact]
        public void Truncated_Hmtx_Does_Not_Throw_From_Advance_Lookup()
        {
            // numberOfHMetrics is unchanged (hundreds) but hmtx is cut to a single metric, so a
            // lookup of a later glyph would slice past the table.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).Truncate("hmtx", 4);

            var typeface = font.TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            var glyph = (ushort)(typeface!.GlyphCount - 1); // well past the surviving metric

            // numberOfHMetrics is clamped to what the table holds, so the lookup reads in range and
            // returns without throwing.
            Assert.Null(Record.Exception(() => typeface.TryGetHorizontalGlyphAdvance(glyph, out _)));
        }
    }
}
