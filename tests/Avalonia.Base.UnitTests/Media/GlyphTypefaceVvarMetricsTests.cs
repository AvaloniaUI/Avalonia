using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// End-to-end tests for VVAR-aware vertical metrics: VVAR threads through
    /// <see cref="GlyphTypeface.TryGetGlyphMetrics(ushort, out GlyphMetrics)"/> and the
    /// batch variant so vertical metrics on a varied typeface reflect the active
    /// variation point.
    /// </summary>
    public class GlyphTypefaceVvarMetricsTests
    {
        private const string AdobeBlankAsset =
            "resm:Avalonia.Base.UnitTests.Assets.AdobeBlank2VF.ttf?assembly=Avalonia.Base.UnitTests";

        private const string InterVariableAsset =
            "resm:Avalonia.Base.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Base.UnitTests";

        private static GlyphTypeface LoadTypeface(string assetUri)
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(assetUri));
            return new GlyphTypeface(new CustomPlatformTypeface(stream));
        }

        [Fact]
        public void TryGetGlyphMetrics_On_Horizontal_Only_Variable_Font_Returns_Same_Height_Across_Variations()
        {
            // Inter Variable has no VVAR, so vertical metrics (Height) are constant
            // across variations. This proves the "no VVAR → no v-side adjustment" path.
            var gt = LoadTypeface(InterVariableAsset);
            var black = gt.WithVariation(gt.CreateNormalizedPosition(new FontVariationSettings(
                new[] { new FontVariation(OpenTypeTag.Parse("wght"), 900) })));

            var glyph = gt.CharacterToGlyphMap['A'];

            Assert.True(gt.TryGetGlyphMetrics(glyph, out var regularMetrics));
            Assert.True(black.TryGetGlyphMetrics(glyph, out var blackMetrics));

            // The advance height (AdvanceHeight) does not change because there's no VVAR.
            // (Ink Height does change at wght=900 via gvar, so the invariant is on the advance.)
            Assert.Equal(regularMetrics.AdvanceHeight, blackMetrics.AdvanceHeight);
        }

        [Fact]
        public void TryGetGlyphMetrics_On_AdobeBlank_Variable_Works_Without_Crashing()
        {
            // AdobeBlank2VF has VVAR. The font is intentionally empty (2 blank glyphs)
            // so we don't assert specific values, but the metrics path must succeed
            // without throwing on a varied typeface — and the batch path must agree
            // with the single-glyph path.
            var gt = LoadTypeface(AdobeBlankAsset);
            var fvarAxes = gt.VariationAxes;
            Assert.Equal(2, fvarAxes.Count);

            // Pick the first axis at its maximum to land at a non-default variation.
            var firstAxisTag = fvarAxes[0].Tag;
            var firstAxisMax = fvarAxes[0].MaximumValue;
            var position = gt.CreateNormalizedPosition(new FontVariationSettings(
                new[] { new FontVariation(firstAxisTag, firstAxisMax) }));
            var varied = gt.WithVariation(position);

            // AdobeBlank ships exactly 2 glyphs (per the binary inspection). Both
            // should return metrics through the varied typeface.
            var glyphs = new ushort[] { 0, 1 };
            for (var i = 0; i < glyphs.Length; i++)
            {
                Assert.True(varied.TryGetGlyphMetrics(glyphs[i], out _));
            }

            var batchMetrics = new GlyphMetrics[glyphs.Length];
            Assert.True(varied.TryGetGlyphMetrics(glyphs, batchMetrics));

            for (var i = 0; i < glyphs.Length; i++)
            {
                Assert.True(varied.TryGetGlyphMetrics(glyphs[i], out var single));
                Assert.Equal(single.AdvanceHeight, batchMetrics[i].AdvanceHeight);
                Assert.Equal(single.YBearing, batchMetrics[i].YBearing);
            }
        }

        [Fact]
        public void TryGetVerticalGlyphAdvance_Applies_Vvar_Like_TryGetGlyphMetrics()
        {
            // R10: the advance-only vertical APIs used to skip VVAR entirely, so a varied clone
            // returned default-instance heights from TryGetVerticalGlyphAdvance(s) while
            // TryGetGlyphMetrics applied VVAR — a horizontal/vertical asymmetry. Both paths must
            // now report the same VVAR-adjusted advance height.
            var gt = LoadTypeface(AdobeBlankAsset);
            var fvarAxes = gt.VariationAxes;
            var position = gt.CreateNormalizedPosition(new FontVariationSettings(
                new[] { new FontVariation(fvarAxes[0].Tag, fvarAxes[0].MaximumValue) }));
            var varied = gt.WithVariation(position);

            var glyphs = new ushort[] { 0, 1 };
            var batch = new ushort[glyphs.Length];
            Assert.True(varied.TryGetVerticalGlyphAdvances(glyphs, batch));

            for (var i = 0; i < glyphs.Length; i++)
            {
                Assert.True(varied.TryGetVerticalGlyphAdvance(glyphs[i], out var single));
                Assert.True(varied.TryGetGlyphMetrics(glyphs[i], out var metrics));

                // Single advance, batch advance, and the metrics advance must agree.
                Assert.Equal(metrics.AdvanceHeight, single);
                Assert.Equal(single, batch[i]);
            }
        }

        [Fact]
        public void TryGetVerticalGlyphAdvances_Applies_Vvar_Region_Scalers()
        {
            // AdobeBlank2VF's only VVAR delta row (glyph 1: +1000) uses the region that peaks
            // on HGHT alone, whose scaler equals the HGHT coordinate, so it cannot tell region
            // scalers from axis coordinates. Point the row at the region that peaks on both
            // axes instead: at wdth = 500, HGHT = 1000 (normalized 0.5, 1) its scaler is 0.5,
            // so glyph 1's advance height is 0 + 0.5 * 1000 = 500.
            var font = SyntheticFont.FromAsset(AdobeBlankAsset);
            Assert.True(LoadTypeface(AdobeBlankAsset).PlatformTypeface.TryGetTable(
                OpenTypeTag.Parse("VVAR"), out var vvarData));

            var vvar = vvarData.ToArray();
            SetFirstRegionIndexOfFirstVarData(vvar, regionIndex: 2);

            var gt = font.Replace("VVAR", vvar).TryCreateGlyphTypeface();
            Assert.NotNull(gt);

            var varied = gt!.WithVariations(new FontVariationSettings(new[]
            {
                new FontVariation(OpenTypeTag.Parse("wdth"), 500),
                new FontVariation(OpenTypeTag.Parse("HGHT"), 1000)
            }));

            var glyphs = new ushort[] { 0, 1 };
            var batch = new ushort[glyphs.Length];
            Assert.True(varied.TryGetVerticalGlyphAdvances(glyphs, batch));

            Assert.True(varied.TryGetVerticalGlyphAdvance(1, out var single));
            Assert.Equal(500, single);
            Assert.Equal(500, batch[1]);
        }

        /// <summary>
        /// Rewrites the first region index of the first ItemVariationData subtable in a VVAR
        /// table's item variation store.
        /// </summary>
        private static void SetFirstRegionIndexOfFirstVarData(byte[] vvar, ushort regionIndex)
        {
            var store = (int)BinaryPrimitives.ReadUInt32BigEndian(vvar.AsSpan(4));

            // ItemVariationStore: format(2), regionListOffset(4), itemVariationDataCount(2),
            // itemVariationDataOffsets(4 each).
            var varData = store + (int)BinaryPrimitives.ReadUInt32BigEndian(vvar.AsSpan(store + 8));

            // ItemVariationData: itemCount(2), wordDeltaCount(2), regionIndexCount(2),
            // regionIndexes(2 each).
            BinaryPrimitives.WriteUInt16BigEndian(vvar.AsSpan(varData + 6), regionIndex);
        }

        [Fact]
        public void TryGetGlyphMetrics_Default_AdobeBlank_Equals_Source()
        {
            // WithVariation(default) on a variable font returns the source itself, so
            // metrics are identical. Pins the source/clone cache-identity contract.
            var gt = LoadTypeface(AdobeBlankAsset);
            var defaultClone = gt.WithVariation(default);

            Assert.Same(gt, defaultClone);

            for (ushort i = 0; i < gt.GlyphCount; i++)
            {
                Assert.True(gt.TryGetGlyphMetrics(i, out var sourceMetrics));
                Assert.True(defaultClone.TryGetGlyphMetrics(i, out var cloneMetrics));
                Assert.Equal(sourceMetrics, cloneMetrics);
            }
        }
    }
}
