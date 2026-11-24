using System;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextShaperVariationTests
    {
        private const string InterVariableAsset =
            "resm:Avalonia.Skia.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string Text = "Hamburg";

        private const double EmSize = 16;

        private static readonly OpenTypeTag s_wghtTag = OpenTypeTag.Parse("wght");

        [Fact]
        public void Shaping_A_Varied_Clone_Uses_The_Variation_Advances()
        {
            using (Start())
            {
                var source = LoadInterVariable();
                var black = source.WithVariations(Weight(900));

                var defaultBuffer = ShapeUnkerned(source);
                var blackBuffer = ShapeUnkerned(black);

                Assert.Equal(Text.Length, blackBuffer.Length);

                for (var i = 0; i < blackBuffer.Length; i++)
                {
                    var glyph = blackBuffer[i].GlyphIndex;

                    Assert.True(black.TryGetHorizontalGlyphAdvance(glyph, out var variedAdvance));

                    var expected = variedAdvance * EmSize / black.Metrics.DesignEmHeight;

                    Assert.Equal(expected, blackBuffer[i].GlyphAdvance, 0.01);
                    Assert.NotEqual(defaultBuffer[i].GlyphAdvance, blackBuffer[i].GlyphAdvance, 0.01);
                }
            }
        }

        [Fact]
        public void Requesting_The_Default_Position_Returns_The_Source_Shaper_Typeface()
        {
            using (Start())
            {
                var source = LoadInterVariable();
                var shaper = source.TextShaperTypeface;

                Assert.Same(shaper, shaper.WithVariation(default));
            }
        }

        [Fact]
        public void Disposing_A_Varied_Clone_Leaves_The_Source_Shaper_Usable()
        {
            using (Start())
            {
                var source = LoadInterVariable();

                var before = TextShaper.Current.ShapeText(Text, new TextShaperOptions(source, EmSize));

                var black = source.WithVariations(Weight(900));
                TextShaper.Current.ShapeText(Text, new TextShaperOptions(black, EmSize));
                black.Dispose();

                var after = TextShaper.Current.ShapeText(Text, new TextShaperOptions(source, EmSize));

                Assert.Equal(before.Length, after.Length);

                for (var i = 0; i < before.Length; i++)
                {
                    Assert.Equal(before[i].GlyphIndex, after[i].GlyphIndex);
                    Assert.Equal(before[i].GlyphAdvance, after[i].GlyphAdvance);
                }
            }
        }

        // Kerning adjusts advances in GPOS, so without it each shaped advance is the glyph's
        // own horizontal advance and can be compared to GlyphTypeface directly.
        private static ShapedBuffer ShapeUnkerned(GlyphTypeface glyphTypeface)
            => TextShaper.Current.ShapeText(Text, new TextShaperOptions(glyphTypeface, EmSize,
                fontFeatures: new[] { FontFeature.Parse("-kern") }));

        private static FontVariationSettings Weight(double value)
            => new(new[] { new FontVariation(s_wghtTag, value) });

        private static GlyphTypeface LoadInterVariable()
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(InterVariableAsset));

            return new GlyphTypeface(new SkiaTypeface(SKTypeface.FromStream(stream), FontSimulations.None));
        }

        private static IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null)));
        }
    }
}
