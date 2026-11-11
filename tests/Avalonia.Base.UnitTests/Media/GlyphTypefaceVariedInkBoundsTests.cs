using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// The ink box of a varied glyf glyph spans its varied points, each rounded to the nearest unit
    /// with halves rounded up, the way FreeType loads a glyph and a static instance stores it. The
    /// box is the same on every path that reports it, whether or not the glyph's outline was built.
    /// </summary>
    public class GlyphTypefaceVariedInkBoundsTests
    {
        private const string Text = "AOgÁé";

        // FreeType 2.13.2's unscaled glyph metrics (FT_LOAD_NO_SCALE) of Inter Variable at opsz 14
        // for A, O, g, Aacute and eacute, as xMin, yMin, xMax, yMax. The wght 900 and 100 boxes are
        // also the glyph headers of the static instances fontTools' varLib.instancer builds.
        private static readonly Dictionary<int, GlyphBounds[]> s_freeTypeBounds = new()
        {
            [900] = new GlyphBounds[]
            {
                new(46, 0, 1582, 1490), new(70, -20, 1518, 1510), new(62, -442, 1224, 1132),
                new(46, 0, 1582, 1954), new(62, -20, 1184, 1582),
            },
            [100] = new GlyphBounds[]
            {
                new(66, 0, 1244, 1490), new(148, -20, 1382, 1510), new(130, -442, 992, 1134),
                new(66, 0, 1244, 1929), new(130, -24, 1050, 1557),
            },
            [550] = new GlyphBounds[]
            {
                new(50, 0, 1421, 1490), new(108, -20, 1464, 1510), new(93, -442, 1132, 1132),
                new(50, 0, 1421, 1936), new(93, -23, 1118, 1564),
            },
            [650] = new GlyphBounds[]
            {
                new(49, 0, 1460, 1490), new(99, -20, 1477, 1510), new(85, -442, 1155, 1132),
                new(49, 0, 1460, 1941), new(85, -22, 1134, 1569),
            },
            [333] = new GlyphBounds[]
            {
                new(55, 0, 1335, 1490), new(128, -20, 1430, 1510), new(110, -442, 1074, 1132),
                new(55, 0, 1335, 1930), new(110, -24, 1084, 1558),
            },
        };

        public static IEnumerable<object[]> Weights()
        {
            foreach (var weight in s_freeTypeBounds.Keys)
            {
                yield return new object[] { weight };
            }
        }

        [Theory]
        [MemberData(nameof(Weights))]
        public void TryGetGlyphMetrics_Reports_The_Box_Of_The_Varied_Points(int weight)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var varied = LoadVaried(weight, out var glyphs);

            for (var i = 0; i < glyphs.Length; i++)
            {
                Assert.True(varied.TryGetGlyphMetrics(glyphs[i], out var metrics));
                AssertInk(s_freeTypeBounds[weight][i], metrics);
            }
        }

        [Theory]
        [MemberData(nameof(Weights))]
        public void Batch_TryGetGlyphMetrics_Reports_The_Box_Of_The_Varied_Points(int weight)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var varied = LoadVaried(weight, out var glyphs);
            var metrics = new GlyphMetrics[glyphs.Length];

            Assert.True(varied.TryGetGlyphMetrics(glyphs, metrics));

            for (var i = 0; i < glyphs.Length; i++)
            {
                AssertInk(s_freeTypeBounds[weight][i], metrics[i]);
            }
        }

        [Theory]
        [MemberData(nameof(Weights))]
        public void TryGetGlyphBounds_Reports_The_Box_Of_The_Varied_Points(int weight)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var varied = LoadVaried(weight, out var glyphs);
            var bounds = new GlyphBounds[glyphs.Length];

            Assert.True(varied.TryGetGlyphBounds(glyphs, bounds));

            Assert.Equal(s_freeTypeBounds[weight], bounds);
        }

        [Theory]
        [MemberData(nameof(Weights))]
        public void The_Box_Does_Not_Depend_On_A_Built_Outline(int weight)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var varied = LoadVaried(weight, out var glyphs);

            for (var i = 0; i < glyphs.Length; i++)
            {
                Assert.NotNull(varied.GetGlyphOutline(glyphs[i]));
                Assert.True(varied.TryGetGlyphMetrics(glyphs[i], out var metrics));
                AssertInk(s_freeTypeBounds[weight][i], metrics);
            }
        }

        private static GlyphTypeface LoadVaried(int weight, out ushort[] glyphs)
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();

            glyphs = new ushort[Text.Length];

            for (var i = 0; i < Text.Length; i++)
            {
                glyphs[i] = typeface.CharacterToGlyphMap[Text[i]];
            }

            return typeface.WithVariations(new FontVariationSettings(
                new[] { new FontVariation(OpenTypeTag.Parse("wght"), weight) }));
        }

        private static void AssertInk(GlyphBounds expected, GlyphMetrics metrics)
        {
            Assert.Equal(expected.XMin, metrics.XBearing);
            Assert.Equal(expected.YMax, metrics.YBearing);
            Assert.Equal(expected.Width, metrics.Width);
            Assert.Equal(expected.Height, metrics.Height);
        }
    }
}
