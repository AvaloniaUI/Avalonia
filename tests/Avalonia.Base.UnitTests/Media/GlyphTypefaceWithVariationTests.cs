using System;
using System.Collections.Generic;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GlyphTypefaceWithVariationTests
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
        public void WithVariation_Returns_Self_For_Static_Font()
        {
            // Static fonts have no axes; any variation request is silently ignored —
            // matches the same policy as CreateVariationSettings (which also returns
            // default for static fonts).
            var gt = LoadTypeface(InterRegularAsset);

            // Construct a non-default settings by hand (CreateVariationSettings would
            // collapse to default on a static font and we want to prove the WithVariation
            // path itself is the one short-circuiting).
            var settings = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 0.5f });

            Assert.Same(gt, gt.WithVariation(settings));
        }

        [Fact]
        public void WithVariation_Returns_Self_For_Default_Settings_On_Variable_Font()
        {
            var gt = LoadTypeface(InterVariableAsset);

            Assert.Same(gt, gt.WithVariation(default));
        }

        [Fact]
        public void WithVariation_Returns_Distinct_Instance_For_NonDefault_Settings()
        {
            var gt = LoadTypeface(InterVariableAsset);

            var bold = WghtSettings(gt, 700f);
            var varied = gt.WithVariation(bold);

            Assert.NotSame(gt, varied);
            Assert.Equal(bold, varied.VariationSettings);
        }

        [Fact]
        public void WithVariation_Returns_Same_Instance_For_Equal_Settings()
        {
            // The cache key is FontVariationSettings, which has structural equality and
            // a precomputed hash. Two equal-but-different value instances must hit the
            // same cache entry.
            var gt = LoadTypeface(InterVariableAsset);

            var a = gt.WithVariation(WghtSettings(gt, 700f));
            var b = gt.WithVariation(WghtSettings(gt, 700f));

            Assert.Same(a, b);
        }

        [Fact]
        public void WithVariation_Returns_Different_Instances_For_Different_Settings()
        {
            var gt = LoadTypeface(InterVariableAsset);

            var bold = gt.WithVariation(WghtSettings(gt, 700f));
            var black = gt.WithVariation(WghtSettings(gt, 900f));

            Assert.NotSame(bold, black);
        }

        [Fact]
        public void WithVariation_On_Clone_Returns_Original_For_Default()
        {
            // Asking a varied clone for the default instance must return the SOURCE — not
            // a new third instance. This is what makes the cache identity work: the
            // source IS the default-instance typeface.
            var gt = LoadTypeface(InterVariableAsset);
            var varied = gt.WithVariation(WghtSettings(gt, 700f));

            Assert.Same(gt, varied.WithVariation(default));
        }

        [Fact]
        public void WithVariation_On_Clone_Returns_Self_For_Same_Settings()
        {
            var gt = LoadTypeface(InterVariableAsset);
            var bold = WghtSettings(gt, 700f);
            var varied = gt.WithVariation(bold);

            // Re-requesting the same variation via the clone must round-trip to the same
            // clone (not allocate a new one).
            Assert.Same(varied, varied.WithVariation(bold));
        }

        [Fact]
        public void WithVariation_On_Clone_Routes_Through_Source_Cache()
        {
            // clone.WithVariation(differentSettings) must produce the same instance as
            // source.WithVariation(differentSettings) — both go through the source's
            // cache and there is only one shared cache per underlying font.
            var gt = LoadTypeface(InterVariableAsset);
            var boldFromSource = gt.WithVariation(WghtSettings(gt, 700f));
            var blackFromSource = gt.WithVariation(WghtSettings(gt, 900f));

            var blackFromClone = boldFromSource.WithVariation(WghtSettings(gt, 900f));

            Assert.Same(blackFromSource, blackFromClone);
        }

        [Fact]
        public void Source_VariationSettings_Is_Default()
        {
            var gt = LoadTypeface(InterVariableAsset);

            Assert.True(gt.VariationSettings.IsDefault);
        }

        [Fact]
        public void Clone_Shares_Parsed_Tables_With_Source()
        {
            // The whole layering trick is that varied clones reference-share parsed
            // tables — they're not re-parsing the font. We can't directly assert
            // reference identity on table fields (they're internal, and some like
            // CharacterToGlyphMap are structs that get boxed on capture). Instead,
            // verify observable consequences: face-level identity is preserved AND
            // glyph lookups produce the same values from a shared cmap.
            var gt = LoadTypeface(InterVariableAsset);
            var varied = gt.WithVariation(WghtSettings(gt, 700f));

            Assert.Equal(gt.FamilyName, varied.FamilyName);
            Assert.Equal(gt.GlyphCount, varied.GlyphCount);
            Assert.Same(gt.FamilyNames, varied.FamilyNames);
            Assert.Same(gt.FaceNames, varied.FaceNames);

            // VariationAxes and NamedInstances are read from the shared fvar table.
            Assert.Equal(gt.VariationAxes, varied.VariationAxes);
            Assert.Equal(gt.NamedInstances.Count, varied.NamedInstances.Count);

            // cmap shares: same glyph index for 'A' on both.
            Assert.True(gt.CharacterToGlyphMap.TryGetGlyph('A', out var sourceGlyph));
            Assert.True(varied.CharacterToGlyphMap.TryGetGlyph('A', out var cloneGlyph));
            Assert.Equal(sourceGlyph, cloneGlyph);
        }

        [Fact]
        public void Clone_Carries_NonDefault_VariationSettings()
        {
            var gt = LoadTypeface(InterVariableAsset);
            var settings = WghtSettings(gt, 700f);
            var varied = gt.WithVariation(settings);

            Assert.False(varied.VariationSettings.IsDefault);
            Assert.Equal(settings, varied.VariationSettings);
        }

        [Fact]
        public void Clone_Shares_PlatformTypeface_With_Source_In_Pr4b()
        {
            // In pr4b the default IPlatformTypeface.WithVariation returns 'this', so the
            // clone reuses the source's platform handle. This pins the contract that
            // tests will break once PR4e1 lands a real SkiaTypeface.WithVariation
            // override — at which point the platform typefaces will diverge and this
            // assertion should be updated to !Same.
            var gt = LoadTypeface(InterVariableAsset);
            var varied = gt.WithVariation(WghtSettings(gt, 700f));

            Assert.Same(gt.PlatformTypeface, varied.PlatformTypeface);
        }

        [Fact]
        public void ToFontCollectionKey_Includes_VariationSettings()
        {
            // FontCollectionKey was extended to carry FontVariationSettings; the
            // extension method must thread it through so cached typefaces under varied
            // keys don't collide with the default-instance entry.
            var gt = LoadTypeface(InterVariableAsset);
            var settings = WghtSettings(gt, 700f);
            var varied = gt.WithVariation(settings);

            var defaultKey = gt.ToFontCollectionKey();
            var variedKey = varied.ToFontCollectionKey();

            Assert.True(defaultKey.Variation.IsDefault);
            Assert.Equal(settings, variedKey.Variation);
            Assert.NotEqual(defaultKey, variedKey);
        }

        [Fact]
        public void FontCollectionKey_Default_Variation_Is_Equal_To_Old_Three_Arg_Key()
        {
            // Equality contract: a key constructed with the old three-arg ctor (Variation
            // left at default) must be equal to the same key built with explicit
            // Variation = default. This is what keeps existing FontCollection cache
            // entries unchanged when varied typefaces aren't involved.
            var threeArg = new FontCollectionKey(FontStyle.Normal, FontWeight.Bold, FontStretch.Normal);
            var fourArg = new FontCollectionKey(FontStyle.Normal, FontWeight.Bold, FontStretch.Normal)
            {
                Variation = default
            };

            Assert.Equal(threeArg, fourArg);
            Assert.Equal(threeArg.GetHashCode(), fourArg.GetHashCode());
        }

        [Fact]
        public void FontCollectionKey_Differs_When_Variation_Differs()
        {
            var gt = LoadTypeface(InterVariableAsset);
            var bold = WghtSettings(gt, 700f);
            var black = WghtSettings(gt, 900f);

            var boldKey = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal)
            {
                Variation = bold
            };
            var blackKey = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal)
            {
                Variation = black
            };

            Assert.NotEqual(boldKey, blackKey);
        }
    }
}
