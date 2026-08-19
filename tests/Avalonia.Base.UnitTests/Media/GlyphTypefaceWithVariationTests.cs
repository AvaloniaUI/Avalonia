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

        private static NormalizedVariationPosition WghtPosition(GlyphTypeface gt, double weight)
            => gt.CreateNormalizedPosition(new FontVariationSettings(
                new[] { new FontVariation(s_wghtTag, weight) }));

        [Fact]
        public void WithVariation_Returns_Self_For_Static_Font()
        {
            // Static fonts have no axes; any variation request is silently ignored —
            // matches the same policy as CreateNormalizedPosition (which also returns
            // default for static fonts).
            var gt = LoadTypeface(InterRegularAsset);

            // Construct a non-default position by hand (CreateNormalizedPosition would
            // collapse to default on a static font and we want to prove the WithVariation
            // path itself is the one short-circuiting).
            var position = NormalizedVariationPosition.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 0.5f });

            Assert.Same(gt, gt.WithVariation(position));
        }

        [Fact]
        public void WithVariation_Returns_Self_For_Default_Position_On_Variable_Font()
        {
            var gt = LoadTypeface(InterVariableAsset);

            Assert.Same(gt, gt.WithVariation(default));
        }

        [Fact]
        public void WithVariation_Returns_Distinct_Instance_For_NonDefault_Position()
        {
            var gt = LoadTypeface(InterVariableAsset);

            var bold = WghtPosition(gt, 700);
            var varied = gt.WithVariation(bold);

            Assert.NotSame(gt, varied);
            Assert.Equal(bold, varied.VariationPosition);
        }

        [Fact]
        public void WithVariation_Returns_Same_Instance_For_Equal_Positions()
        {
            // The cache key is NormalizedVariationPosition, which has structural equality
            // and a precomputed hash. Two equal-but-different value instances must hit
            // the same cache entry.
            var gt = LoadTypeface(InterVariableAsset);

            var a = gt.WithVariation(WghtPosition(gt, 700));
            var b = gt.WithVariation(WghtPosition(gt, 700));

            Assert.Same(a, b);
        }

        [Fact]
        public void WithVariation_Returns_Different_Instances_For_Different_Positions()
        {
            var gt = LoadTypeface(InterVariableAsset);

            var bold = gt.WithVariation(WghtPosition(gt, 700));
            var black = gt.WithVariation(WghtPosition(gt, 900));

            Assert.NotSame(bold, black);
        }

        [Fact]
        public void WithVariation_On_Clone_Returns_Original_For_Default()
        {
            // Asking a varied clone for the default instance must return the SOURCE — not
            // a new third instance. This is what makes the cache identity work: the
            // source IS the default-instance typeface.
            var gt = LoadTypeface(InterVariableAsset);
            var varied = gt.WithVariation(WghtPosition(gt, 700));

            Assert.Same(gt, varied.WithVariation(default));
        }

        [Fact]
        public void WithVariation_On_Clone_Returns_Self_For_Same_Position()
        {
            var gt = LoadTypeface(InterVariableAsset);
            var bold = WghtPosition(gt, 700);
            var varied = gt.WithVariation(bold);

            // Re-requesting the same variation via the clone must round-trip to the same
            // clone (not allocate a new one).
            Assert.Same(varied, varied.WithVariation(bold));
        }

        [Fact]
        public void WithVariation_On_Clone_Routes_Through_Source_Cache()
        {
            // clone.WithVariation(differentPosition) must produce the same instance as
            // source.WithVariation(differentPosition) — both go through the source's
            // cache and there is only one shared cache per underlying font.
            var gt = LoadTypeface(InterVariableAsset);
            var boldFromSource = gt.WithVariation(WghtPosition(gt, 700));
            var blackFromSource = gt.WithVariation(WghtPosition(gt, 900));

            var blackFromClone = boldFromSource.WithVariation(WghtPosition(gt, 900));

            Assert.Same(blackFromSource, blackFromClone);
        }

        [Fact]
        public void Source_VariationPosition_Is_Default()
        {
            var gt = LoadTypeface(InterVariableAsset);

            Assert.True(gt.VariationPosition.IsDefault);
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
            var varied = gt.WithVariation(WghtPosition(gt, 700));

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
        public void Clone_Carries_NonDefault_VariationPosition()
        {
            var gt = LoadTypeface(InterVariableAsset);
            var position = WghtPosition(gt, 700);
            var varied = gt.WithVariation(position);

            Assert.False(varied.VariationPosition.IsDefault);
            Assert.Equal(position, varied.VariationPosition);
        }

        [Fact]
        public void Clone_Shares_PlatformTypeface_With_Source_When_Override_Is_No_Op()
        {
            // With the default no-op IPlatformTypeface.WithVariation override, the clone
            // reuses the source's platform handle. This pins the current behavior; when
            // a platform implementation overrides WithVariation to actually clone the
            // underlying face (e.g. via SKTypeface.Clone), this assertion should be
            // updated to !Same against that platform.
            var gt = LoadTypeface(InterVariableAsset);
            var varied = gt.WithVariation(WghtPosition(gt, 700));

            Assert.Same(gt.PlatformTypeface, varied.PlatformTypeface);
        }

        [Fact]
        public void ToFontCollectionKey_Includes_VariationPosition()
        {
            // FontCollectionKey was extended to carry the normalized variation position;
            // the extension method must thread it through so cached typefaces under
            // varied keys don't collide with the default-instance entry.
            var gt = LoadTypeface(InterVariableAsset);
            var position = WghtPosition(gt, 700);
            var varied = gt.WithVariation(position);

            var defaultKey = gt.ToFontCollectionKey();
            var variedKey = varied.ToFontCollectionKey();

            Assert.True(defaultKey.Variation.IsDefault);
            Assert.Equal(position, variedKey.Variation);
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
        public void WithVariations_Normalizes_User_Space_Settings()
        {
            // The public front door: user-space wght=700 must land on the same clone as
            // the internal path that normalizes explicitly.
            var gt = LoadTypeface(InterVariableAsset);

            var viaPublic = gt.WithVariations(FontVariationSettings.Parse("wght=700"));
            var viaInternal = gt.WithVariation(WghtPosition(gt, 700));

            Assert.Same(viaInternal, viaPublic);
            Assert.NotSame(gt, viaPublic);
        }

        [Fact]
        public void WithVariations_Returns_Self_For_Null_And_Empty()
        {
            var gt = LoadTypeface(InterVariableAsset);

            Assert.Same(gt, gt.WithVariations(null));
            Assert.Same(gt, gt.WithVariations(FontVariationSettings.Empty));
        }

        [Fact]
        public void WithVariations_Returns_Self_For_Static_Font()
        {
            var gt = LoadTypeface(InterRegularAsset);

            Assert.Same(gt, gt.WithVariations(FontVariationSettings.Parse("wght=700")));
        }

        [Fact]
        public void WithVariations_Shares_Clones_Between_Settings_That_Normalize_Equal()
        {
            // Clamping happens during normalization, BEFORE the cache lookup — so two
            // different user-space requests that clamp to the same axis position must
            // share one clone instead of polluting the cache with duplicates.
            var gt = LoadTypeface(InterVariableAsset);

            var atMax = gt.WithVariations(FontVariationSettings.Parse("wght=900"));
            var clamped = gt.WithVariations(FontVariationSettings.Parse("wght=10000"));

            Assert.Same(atMax, clamped);
        }

        [Fact]
        public void WithVariations_Selects_Named_Instance_By_Index()
        {
            // Instance index 8 in Inter Variable is Black (wght=900); selecting it by
            // index must land on the same clone as requesting wght=900 explicitly.
            var gt = LoadTypeface(InterVariableAsset);
            var lastIndex = gt.NamedInstances.Count - 1;

            var byIndex = gt.WithVariations(null, instanceIndex: lastIndex);
            var byValues = gt.WithVariations(FontVariationSettings.Parse("wght=900"));

            Assert.Same(byValues, byIndex);
        }

        [Fact]
        public void FontCollectionKey_Differs_When_Variation_Differs()
        {
            var gt = LoadTypeface(InterVariableAsset);
            var bold = WghtPosition(gt, 700);
            var black = WghtPosition(gt, 900);

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
