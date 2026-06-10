using System;
using Avalonia.Media.Fonts.Tables.Colr;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// COLR v1 variation resolves through the shared <c>Tables.Variation.ItemVariationStore</c>: a
    /// paint record's <c>VarIndexBase</c> (plus a per-field offset) maps to an (outer, inner) delta,
    /// which is scaled by the variation region's contribution at the instance's coordinates. This
    /// pins the region scaling and the index resolution that the COLR-local store used to lack.
    /// </summary>
    public class ColrVariationCharacterizationTests
    {
        [Fact]
        public void Scaled_Delta_Applies_Region_Scaling_At_The_Active_Coords()
        {
            const short rawDelta = 100;

            // The store's axis count must match the font's fvar, so size the region from the real font.
            var axisCount = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable)
                .TryCreateGlyphTypeface()!.VariationAxes.Count;
            Assert.True(axisCount >= 1);

            var typeface = ColrTestFont
                .Graft(SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable),
                    BuildColrWithItemVariationStore(rawDelta, axisCount))
                .TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            Assert.True(ColrTable.TryLoad(typeface!, out var colrTable));

            // The single region peaks on axis 0: at the peak (coord 1.0) its scaler is 1, so the
            // resolved delta is the raw stored value; at the default (coord 0) the scaler is 0.
            var atPeak = new float[axisCount];
            atPeak[0] = 1f;
            var atDefault = new float[axisCount];

            Assert.True(colrTable!.TryGetScaledDelta(varIndexBase: 0, fieldOffset: 0, atPeak, out var deltaAtPeak));
            Assert.True(Math.Abs(deltaAtPeak - rawDelta) < 0.01f, $"expected ~{rawDelta} at the region peak, got {deltaAtPeak}");

            Assert.True(colrTable.TryGetScaledDelta(varIndexBase: 0, fieldOffset: 0, atDefault, out var deltaAtDefault));
            Assert.True(Math.Abs(deltaAtDefault) < 0.01f, $"expected ~0 at the default instance, got {deltaAtDefault}");

            // The field offset shifts the variation index; index 1 isn't in the store, so no delta.
            Assert.False(colrTable.TryGetScaledDelta(varIndexBase: 0, fieldOffset: 1, atPeak, out _));

            // 0xFFFFFFFF is the "no variation" sentinel.
            Assert.False(colrTable.TryGetScaledDelta(varIndexBase: 0xFFFFFFFF, fieldOffset: 0, atPeak, out _));
        }

        /// <summary>
        /// Builds a minimal COLR v1 table whose only populated field is an ItemVariationStore: one
        /// region peaking on axis 0, one ItemVariationData with a single item carrying one word delta.
        /// No DeltaSetIndexMap, so the implicit (outer = index &gt;&gt; 16, inner = index &amp; 0xFFFF)
        /// mapping applies — index 0 → (0, 0).
        /// </summary>
        private static byte[] BuildColrWithItemVariationStore(short rawDelta, int axisCount)
        {
            // --- ItemVariationStore ---
            var ivs = new BigEndianBuffer();
            ivs.UInt16(1);                              // format
            var regionListOffsetPos = ivs.ReserveOffset32();
            ivs.UInt16(1);                              // itemVariationDataCount
            var ivdOffsetPos = ivs.ReserveOffset32();   // itemVariationDataOffsets[0]

            // VariationRegionList: one region. Axis 0 ramps 0 → peak 1.0 → end 1.0; other axes flat.
            ivs.PatchUInt32(regionListOffsetPos, (uint)ivs.Position);
            ivs.UInt16((ushort)axisCount);              // axisCount
            ivs.UInt16(1);                              // regionCount
            for (var a = 0; a < axisCount; a++)
            {
                if (a == 0)
                {
                    ivs.Int16(0).Int16(16384).Int16(16384); // start, peak (F2DOT14 1.0), end
                }
                else
                {
                    ivs.Int16(0).Int16(0).Int16(0);         // no contribution
                }
            }

            // ItemVariationData: 1 item, 1 word delta over region 0.
            ivs.PatchUInt32(ivdOffsetPos, (uint)ivs.Position);
            ivs.UInt16(1);          // itemCount
            ivs.UInt16(1);          // wordDeltaCount (no LONG_WORDS flag)
            ivs.UInt16(1);          // regionIndexCount
            ivs.UInt16(0);          // regionIndexes[0] = region 0
            ivs.Int16(rawDelta);    // deltaSets[0] = one int16 word delta

            var ivsBytes = ivs.ToArray();

            // --- COLR v1 header (only itemVariationStoreOffset populated) ---
            var colr = new BigEndianBuffer();
            colr.UInt16(1);   // version
            colr.UInt16(0);   // numBaseGlyphRecords
            colr.UInt32(0);   // baseGlyphRecordsOffset
            colr.UInt32(0);   // layerRecordsOffset
            colr.UInt16(0);   // numLayerRecords
            colr.UInt32(0);   // baseGlyphV1ListOffset
            colr.UInt32(0);   // layerV1ListOffset
            colr.UInt32(0);   // clipListOffset
            colr.UInt32(0);   // varIndexMapOffset (0 → implicit mapping)
            var ivsOffsetPos = colr.ReserveOffset32(); // itemVariationStoreOffset
            colr.PatchUInt32(ivsOffsetPos, (uint)colr.Position);
            colr.Bytes(ivsBytes);

            return colr.ToArray();
        }
    }
}
