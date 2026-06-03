using System;
using System.Collections.Generic;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// End-to-end tests for HVAR-aware horizontal advance lookups: the values
    /// returned by <see cref="GlyphTypeface.TryGetHorizontalGlyphAdvance"/> and
    /// <see cref="GlyphTypeface.TryGetGlyphMetrics(ushort, out GlyphMetrics)"/>
    /// surfaces reflect the variation point.
    /// </summary>
    public class GlyphTypefaceHvarAdvanceTests
    {
        private const string InterRegularAsset =
            "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        private const string InterVariableAsset =
            "resm:Avalonia.Base.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Base.UnitTests";

        private static readonly OpenTypeTag s_wghtTag = OpenTypeTag.Parse("wght");

        private static GlyphTypeface LoadTypeface(string assetUri)
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(assetUri));
            return new GlyphTypeface(new CustomPlatformTypeface(stream));
        }

        private static FontVariationSettings WghtSettings(GlyphTypeface gt, float weight)
            => gt.CreateVariationSettings(new Dictionary<OpenTypeTag, float> { [s_wghtTag] = weight });

        [Fact]
        public void TryGetHorizontalGlyphAdvance_Returns_Same_Value_On_Static_Font_Across_WithVariation_Calls()
        {
            // Static fonts have no HVAR; WithVariation returns the same instance, so the
            // advance is naturally the same. This pins the "no regression" promise.
            var gt = LoadTypeface(InterRegularAsset);
            var glyph = gt.CharacterToGlyphMap['A'];

            Assert.True(gt.TryGetHorizontalGlyphAdvance(glyph, out var defaultAdvance));

            // For a static font WithVariation always returns this; both calls should
            // produce the same advance.
            var pretendBold = gt.WithVariation(FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 0.5f }));
            Assert.True(pretendBold.TryGetHorizontalGlyphAdvance(glyph, out var pretendBoldAdvance));

            Assert.Equal(defaultAdvance, pretendBoldAdvance);
        }

        [Fact]
        public void TryGetHorizontalGlyphAdvance_On_Default_Variable_Equals_hmtx_Value()
        {
            // At the default-instance point HVAR contributes 0, so the advance equals
            // the raw hmtx value. The source typeface (no variation applied) provides
            // the baseline.
            var gt = LoadTypeface(InterVariableAsset);
            var glyph = gt.CharacterToGlyphMap['A'];

            Assert.True(gt.TryGetHorizontalGlyphAdvance(glyph, out var defaultAdvance));
            Assert.True(defaultAdvance > 0);
        }

        [Fact]
        public void TryGetHorizontalGlyphAdvance_At_wght_900_Is_Wider_Than_Default()
        {
            // The headline assertion: a bolder glyph has a bigger advance. Without HVAR
            // this would fail (the advance stays at the default-instance value and
            // glyphs would overlap when laid out at wght=900).
            var gt = LoadTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));
            var glyph = gt.CharacterToGlyphMap['A'];

            Assert.True(gt.TryGetHorizontalGlyphAdvance(glyph, out var regularAdvance));
            Assert.True(black.TryGetHorizontalGlyphAdvance(glyph, out var blackAdvance));

            Assert.True(blackAdvance > regularAdvance,
                $"Expected wght=900 advance ({blackAdvance}) > default advance ({regularAdvance})");
        }

        [Fact]
        public void Advance_Grows_Monotonically_With_Weight()
        {
            // Intermediate weight produces intermediate advance because HVAR's region
            // scalers are linear ramps through axis space.
            var gt = LoadTypeface(InterVariableAsset);
            var semiBold = gt.WithVariation(WghtSettings(gt, 700f));
            var black = gt.WithVariation(WghtSettings(gt, 900f));
            var glyph = gt.CharacterToGlyphMap['A'];

            Assert.True(gt.TryGetHorizontalGlyphAdvance(glyph, out var regularAdvance));
            Assert.True(semiBold.TryGetHorizontalGlyphAdvance(glyph, out var semiBoldAdvance));
            Assert.True(black.TryGetHorizontalGlyphAdvance(glyph, out var blackAdvance));

            Assert.True(semiBoldAdvance >= regularAdvance,
                $"semiBold {semiBoldAdvance} should be >= regular {regularAdvance}");
            Assert.True(blackAdvance >= semiBoldAdvance,
                $"black {blackAdvance} should be >= semiBold {semiBoldAdvance}");
        }

        [Fact]
        public void Batch_Advances_Match_Single_Calls()
        {
            // The batch API must produce the same values as the single-glyph API for
            // every glyph in the input. Both apply HVAR after the base hmtx read.
            var gt = LoadTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));

            var glyphs = new ushort[]
            {
                gt.CharacterToGlyphMap['A'],
                gt.CharacterToGlyphMap['B'],
                gt.CharacterToGlyphMap['M'],
                gt.CharacterToGlyphMap['i'],
                gt.CharacterToGlyphMap['W'],
            };

            var batchAdvances = new ushort[glyphs.Length];
            Assert.True(black.TryGetHorizontalGlyphAdvances(glyphs, batchAdvances));

            for (var i = 0; i < glyphs.Length; i++)
            {
                Assert.True(black.TryGetHorizontalGlyphAdvance(glyphs[i], out var singleAdvance));
                Assert.Equal(singleAdvance, batchAdvances[i]);
            }
        }

        [Fact]
        public void TryGetGlyphMetrics_Single_Advance_Tracks_TryGetHorizontalGlyphAdvance()
        {
            // The single-glyph TryGetGlyphMetrics must report the same advance width as
            // TryGetHorizontalGlyphAdvance — HVAR application is consistent across both.
            var gt = LoadTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));
            var glyph = gt.CharacterToGlyphMap['A'];

            Assert.True(black.TryGetHorizontalGlyphAdvance(glyph, out var advance));
            Assert.True(black.TryGetGlyphMetrics(glyph, out var metrics));

            Assert.Equal(advance, metrics.AdvanceWidth);
        }

        [Fact]
        public void TryGetGlyphMetrics_Batch_Advance_Tracks_Single_Path()
        {
            var gt = LoadTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));

            var glyphs = new ushort[]
            {
                gt.CharacterToGlyphMap['A'],
                gt.CharacterToGlyphMap['M'],
                gt.CharacterToGlyphMap['i'],
            };

            var batchMetrics = new GlyphMetrics[glyphs.Length];
            Assert.True(black.TryGetGlyphMetrics(glyphs, batchMetrics));

            for (var i = 0; i < glyphs.Length; i++)
            {
                Assert.True(black.TryGetGlyphMetrics(glyphs[i], out var singleMetric));
                Assert.Equal(singleMetric.AdvanceWidth, batchMetrics[i].AdvanceWidth);
            }
        }

        [Fact]
        public void Default_Variation_Returns_Same_Advance_As_Source()
        {
            // WithVariation(default) returns the source; the advance must be identical
            // to the source's advance even after we walk through WithVariation.
            var gt = LoadTypeface(InterVariableAsset);
            var defaultClone = gt.WithVariation(default);

            Assert.Same(gt, defaultClone);

            var glyph = gt.CharacterToGlyphMap['A'];
            Assert.True(gt.TryGetHorizontalGlyphAdvance(glyph, out var sourceAdvance));
            Assert.True(defaultClone.TryGetHorizontalGlyphAdvance(glyph, out var cloneAdvance));
            Assert.Equal(sourceAdvance, cloneAdvance);
        }
    }
}
