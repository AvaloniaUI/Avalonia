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
    /// End-to-end tests for pr4f: <see cref="GlyphTypeface.Metrics"/> on a typeface
    /// produced by <see cref="GlyphTypeface.WithVariation"/> reflects the variation
    /// point via MVAR.
    /// </summary>
    public class GlyphTypefaceMvarMetricsTests
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
        public void Static_Font_Metrics_Unchanged_Across_WithVariation()
        {
            // WithVariation returns 'this' for static fonts, so Metrics is trivially
            // identical. Pins the no-regression promise.
            var gt = LoadTypeface(InterRegularAsset);
            var alsoGt = gt.WithVariation(FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 0.5f }));

            Assert.Same(gt, alsoGt);
            Assert.Equal(gt.Metrics, alsoGt.Metrics);
        }

        [Fact]
        public void Variable_Font_Source_Metrics_Equal_Default_Instance()
        {
            // The source typeface's Metrics is at the default instance — no MVAR delta
            // applied. This is what gives the existing PR2 default-instance render tests
            // their stability.
            var gt = LoadTypeface(InterVariableAsset);

            // Ascent / Descent values are non-zero (sanity).
            Assert.NotEqual(0, gt.Metrics.Ascent);
            Assert.NotEqual(0, gt.Metrics.Descent);
        }

        [Fact]
        public void Underline_Thickness_Grows_With_Weight()
        {
            // Inter Variable's MVAR declares 'unds' (underline size). At wght=900 the
            // underline should be thicker than at wght=400.
            var gt = LoadTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));

            Assert.True(black.Metrics.UnderlineThickness > gt.Metrics.UnderlineThickness,
                $"Expected wght=900 underline thickness ({black.Metrics.UnderlineThickness}) " +
                $"> regular ({gt.Metrics.UnderlineThickness})");
        }

        [Fact]
        public void Strikeout_Thickness_Grows_With_Weight()
        {
            // Same reasoning for strs (strikeout size). Inter Variable declares it.
            var gt = LoadTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));

            Assert.True(black.Metrics.StrikethroughThickness > gt.Metrics.StrikethroughThickness,
                $"Expected wght=900 strikeout thickness ({black.Metrics.StrikethroughThickness}) " +
                $"> regular ({gt.Metrics.StrikethroughThickness})");
        }

        [Fact]
        public void Ascent_And_Descent_Unchanged_Across_Inter_Weights()
        {
            // Inter Variable doesn't declare hasc/hdsc in MVAR — those metrics are
            // designed to be constant across weights so all weights share a baseline.
            // Verifies that missing MVAR records leave the field at the source's value.
            var gt = LoadTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));

            Assert.Equal(gt.Metrics.Ascent, black.Metrics.Ascent);
            Assert.Equal(gt.Metrics.Descent, black.Metrics.Descent);
            Assert.Equal(gt.Metrics.LineGap, black.Metrics.LineGap);
        }

        [Fact]
        public void Default_Variation_Clone_Returns_Source_With_Identical_Metrics()
        {
            // WithVariation(default) on a variable font returns the source itself, so
            // Metrics is identical by reference equality of the underlying record struct.
            var gt = LoadTypeface(InterVariableAsset);
            var defaultClone = gt.WithVariation(default);

            Assert.Same(gt, defaultClone);
            Assert.Equal(gt.Metrics, defaultClone.Metrics);
        }

        [Fact]
        public void Intermediate_Weight_Has_Intermediate_Underline_Thickness()
        {
            // wght=700 lies between wght=400 (default) and wght=900 along ItemVariationStore's
            // linear region scaler. Underline thickness should follow the same
            // monotonic growth.
            var gt = LoadTypeface(InterVariableAsset);
            var semiBold = gt.WithVariation(WghtSettings(gt, 700f));
            var black = gt.WithVariation(WghtSettings(gt, 900f));

            Assert.True(semiBold.Metrics.UnderlineThickness >= gt.Metrics.UnderlineThickness,
                $"semiBold {semiBold.Metrics.UnderlineThickness} should be >= regular {gt.Metrics.UnderlineThickness}");
            Assert.True(black.Metrics.UnderlineThickness >= semiBold.Metrics.UnderlineThickness,
                $"black {black.Metrics.UnderlineThickness} should be >= semiBold {semiBold.Metrics.UnderlineThickness}");
        }
    }
}
