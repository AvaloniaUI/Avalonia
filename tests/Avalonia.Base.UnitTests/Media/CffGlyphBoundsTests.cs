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
    /// Skia-free tests for CFF / CFF2 control-point ink bounds. <see cref="GlyphTypeface.TryGetGlyphBounds"/>
    /// and the <see cref="GlyphMetrics"/> ink box are computed by interpreting the charstring into a
    /// bounds-accumulating sink, so — unlike <c>GetGlyphOutline().Bounds</c>, which needs a render
    /// interface for the geometry — they run with no platform backend registered.
    /// </summary>
    /// <remarks>
    /// Ground truth is fontTools' <c>ControlBoundsPen</c> (the box over every on- and off-curve point),
    /// with the minima floored and the maxima ceiled to match <c>BoundsGeometryContext</c>. For the
    /// integer-coordinate fonts the match is exact; CFF2 blends evaluate with float region scalers, so a
    /// small tolerance absorbs sub-unit rounding at the variation point.
    /// </remarks>
    public class CffGlyphBoundsTests
    {
        // CffTest / CidTest are synthetic; SourceCodePro and AdobeVFPrototype are shipping Adobe fonts
        // (a plain CFF and a CFF2 variable font respectively), embedded from the render-test assets.
        private const string CffAsset = "resm:Avalonia.Base.UnitTests.Assets.CffTest.otf?assembly=Avalonia.Base.UnitTests";
        private const string CidAsset = "resm:Avalonia.Base.UnitTests.Assets.CidTest.otf?assembly=Avalonia.Base.UnitTests";
        private const string SourceCodeProAsset = "resm:Avalonia.Base.UnitTests.Assets.SourceCodePro-Subset.otf?assembly=Avalonia.Base.UnitTests";
        private const string AdobeVfAsset = "resm:Avalonia.Base.UnitTests.Assets.AdobeVFPrototype-Subset.otf?assembly=Avalonia.Base.UnitTests";

        private static readonly OpenTypeTag s_wghtTag = OpenTypeTag.Parse("wght");

        private static GlyphTypeface LoadTypeface(string assetUri)
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(assetUri));
            return new GlyphTypeface(new CustomPlatformTypeface(stream));
        }

        private static GlyphBounds BoundsOf(GlyphTypeface gt, char ch)
        {
            var glyphs = new[] { gt.CharacterToGlyphMap[ch] };
            var bounds = new GlyphBounds[1];

            Assert.True(gt.TryGetGlyphBounds(glyphs, bounds));

            return bounds[0];
        }

        private static void AssertBounds(GlyphBounds actual, int xMin, int yMin, int xMax, int yMax, int tolerance = 2)
        {
            Assert.InRange(actual.XMin, xMin - tolerance, xMin + tolerance);
            Assert.InRange(actual.YMin, yMin - tolerance, yMin + tolerance);
            Assert.InRange(actual.XMax, xMax - tolerance, xMax + tolerance);
            Assert.InRange(actual.YMax, yMax - tolerance, yMax + tolerance);
        }

        [Theory]
        [InlineData(CffAsset, GlyphOutlineType.Cff)]
        [InlineData(CidAsset, GlyphOutlineType.Cff)]
        [InlineData(SourceCodeProAsset, GlyphOutlineType.Cff)]
        [InlineData(AdobeVfAsset, GlyphOutlineType.Cff2)]
        public void OutlineType_Reflects_The_Outline_Table(string asset, GlyphOutlineType expected)
        {
            var gt = LoadTypeface(asset);

            Assert.Equal(expected, gt.OutlineType);
        }

        // --- Synthetic non-CID CFF: exact integer control bounds (lines + cardinal-point arcs). ---

        [Theory]
        [InlineData('I', 400, 0, 600, 700)]
        [InlineData('L', 200, 0, 600, 700)]
        [InlineData('O', 100, 50, 700, 650)]
        public void CffTest_Control_Bounds_Match_FontTools(char ch, int xMin, int yMin, int xMax, int yMax)
        {
            var gt = LoadTypeface(CffAsset);

            AssertBounds(BoundsOf(gt, ch), xMin, yMin, xMax, yMax);
        }

        // --- CID-keyed CFF: same outlines reached through FDSelect + FDArray local subrs. ---

        [Theory]
        [InlineData('I', 400, 0, 600, 700)]
        [InlineData('O', 100, 50, 700, 650)]
        public void CidTest_Control_Bounds_Match_FontTools(char ch, int xMin, int yMin, int xMax, int yMax)
        {
            var gt = LoadTypeface(CidAsset);

            AssertBounds(BoundsOf(gt, ch), xMin, yMin, xMax, yMax);
        }

        // --- Shipping CFF (Adobe Source Code Pro): real cubic curves with off-curve extrema. ---

        [Theory]
        [InlineData('H', 79, 0, 521, 656)]
        [InlineData('O', 48, -12, 552, 668)]
        [InlineData('a', 81, -12, 515, 498)]
        [InlineData('g', 72, -224, 566, 498)]
        public void SourceCodePro_Control_Bounds_Match_FontTools(char ch, int xMin, int yMin, int xMax, int yMax)
        {
            var gt = LoadTypeface(SourceCodeProAsset);

            AssertBounds(BoundsOf(gt, ch), xMin, yMin, xMax, yMax);
        }

        // --- Shipping CFF2 (Adobe Variable Font Prototype) at the default instance (blends at origin). ---

        [Theory]
        [InlineData('H', 45, 0, 745, 670)]
        [InlineData('A', 5, 0, 653, 675)]
        [InlineData('o', 46, -13, 502, 487)]
        public void AdobeVf_Default_Control_Bounds_Match_FontTools(char ch, int xMin, int yMin, int xMax, int yMax)
        {
            var gt = LoadTypeface(AdobeVfAsset);

            AssertBounds(BoundsOf(gt, ch), xMin, yMin, xMax, yMax);
        }

        // --- CFF2 at wght=900: blend evaluated at the clone's active variation coordinates. ---

        [Theory]
        [InlineData('H', 28, 0, 728, 652)]
        [InlineData('A', 10, 0, 665, 652)]
        [InlineData('o', 22, -16, 550, 503)]
        public void AdobeVf_Wght900_Control_Bounds_Track_The_Variation_Point(char ch, int xMin, int yMin, int xMax, int yMax)
        {
            var gt = LoadTypeface(AdobeVfAsset);
            var black = gt.WithVariation(gt.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 900f }));

            AssertBounds(BoundsOf(black, ch), xMin, yMin, xMax, yMax);
        }

        [Fact]
        public void AdobeVf_Bounds_Move_Between_Default_And_Wght900()
        {
            var gt = LoadTypeface(AdobeVfAsset);
            var black = gt.WithVariation(gt.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 900f }));

            // The blend must actually deform the outline: a heavier 'o' has a different control box.
            Assert.NotEqual(BoundsOf(gt, 'o'), BoundsOf(black, 'o'));
        }

        [Fact]
        public void Repeated_Reads_Return_The_Same_Bounds()
        {
            var gt = LoadTypeface(SourceCodeProAsset);

            // The first read interprets the charstring and memoises it; every later read of the same
            // glyph hits the cache and must return the identical box.
            var first = BoundsOf(gt, 'H');

            for (var i = 0; i < 5; i++)
            {
                Assert.Equal(first, BoundsOf(gt, 'H'));
            }
        }

        [Fact]
        public void Out_Of_Range_Glyph_Yields_Zero_Bounds()
        {
            var gt = LoadTypeface(SourceCodeProAsset);
            var glyphs = new ushort[] { ushort.MaxValue };
            var bounds = new GlyphBounds[1];

            // The font has an outline table, so the batch call still succeeds as a whole...
            Assert.True(gt.TryGetGlyphBounds(glyphs, bounds));

            // ...but a glyph past the charstring count is written as the default (zero) box.
            Assert.Equal(default, bounds[0]);
        }

        // --- The metrics ink box is fed by the same CFF/CFF2 bounds computation. ---

        [Theory]
        [InlineData(SourceCodeProAsset, 'H')]
        [InlineData(AdobeVfAsset, 'H')]
        public void TryGetGlyphMetrics_Ink_Box_Matches_TryGetGlyphBounds(string asset, char ch)
        {
            var gt = LoadTypeface(asset);
            var glyph = gt.CharacterToGlyphMap[ch];

            Assert.True(gt.TryGetGlyphMetrics(glyph, out var metrics));

            var bounds = BoundsOf(gt, ch);

            // An inked glyph reports a non-zero ink box, and Width/Height are exactly the
            // control-bounds extents — TryGetGlyphMetrics and TryGetGlyphBounds share one path.
            Assert.True(metrics.Width > 0);
            Assert.True(metrics.Height > 0);
            Assert.Equal(bounds.Width, metrics.Width);
            Assert.Equal(bounds.Height, metrics.Height);
            Assert.Equal(bounds.XMin, metrics.XBearing);
            Assert.Equal(bounds.YMax, metrics.YBearing);
        }

        [Fact]
        public void TryGetGlyphMetrics_Batch_Matches_Single_For_Cff()
        {
            var gt = LoadTypeface(SourceCodeProAsset);
            var glyphs = new ushort[]
            {
                gt.CharacterToGlyphMap['H'],
                gt.CharacterToGlyphMap['O'],
                gt.CharacterToGlyphMap['a'],
                gt.CharacterToGlyphMap['g'],
                gt.CharacterToGlyphMap[' '],
            };

            var batch = new GlyphMetrics[glyphs.Length];
            Assert.True(gt.TryGetGlyphMetrics(glyphs, batch));

            for (var i = 0; i < glyphs.Length; i++)
            {
                Assert.True(gt.TryGetGlyphMetrics(glyphs[i], out var single));

                // GlyphMetrics is a record struct: structural equality across both paths.
                Assert.Equal(single, batch[i]);
            }
        }
    }
}
