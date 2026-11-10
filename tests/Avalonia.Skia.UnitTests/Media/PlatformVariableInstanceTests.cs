#nullable enable

using System;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    /// <summary>
    /// DirectWrite and fontconfig answer a weight or stretch request on a variable font with a
    /// named instance applied inside the typeface, while the font tables the typeface hands out
    /// describe the default instance. The glyph typeface has to read the font at the instance the
    /// platform draws, which a variation of the family's default face at that named instance
    /// reads from the same tables.
    /// </summary>
    public class PlatformVariableInstanceTests
    {
        private const string Text = "Hamburg $0";

        [Win32Fact("Windows specific font")]
        public void Should_Shape_A_DirectWrite_Instance_With_The_Advances_Of_Its_Named_Instance()
        {
            using (Start())
            {
                AssertShapedLikeNamedInstance("Bahnschrift", FontWeight.Bold, FontStretch.Condensed, "Bahnschrift",
                    "Bold Condensed");
                AssertShapedLikeNamedInstance("Bahnschrift", FontWeight.Light, FontStretch.SemiCondensed,
                    "Bahnschrift", "Light SemiCondensed");
                AssertShapedLikeNamedInstance("Segoe UI Variable Text", FontWeight.SemiBold, FontStretch.Normal,
                    "Segoe UI Variable Text", "Semibold Text");
                AssertShapedLikeNamedInstance("Segoe UI Variable Text", FontWeight.Bold, FontStretch.Normal,
                    "Segoe UI Variable Text", "Bold Text");

                // The Regular faces of the Display and Small families sit at another optical size than
                // the font's default instance, so even a regular request gets a named instance.
                AssertShapedLikeNamedInstance("Segoe UI Variable Display", FontWeight.Normal, FontStretch.Normal,
                    "Segoe UI Variable Text", "Regular Display");
                AssertShapedLikeNamedInstance("Segoe UI Variable Small", FontWeight.Bold, FontStretch.Normal,
                    "Segoe UI Variable Text", "Bold Small");
            }
        }

        [Win32Fact("Windows specific font")]
        public void Should_Shape_A_DirectWrite_Instance_Of_A_Font_Whose_Default_Is_Not_Regular()
        {
            using (Start())
            {
                // Noto Sans KR's default instance is Thin, so a regular request is itself a named
                // instance and every weight read at the default would shape at Thin widths.
                var thin = GetPlatformFace("Noto Sans KR", FontWeight.Thin, FontStretch.Normal);

                Assert.True(thin.VariationPosition.IsDefault);

                foreach (var (weight, instanceName) in new[] { (FontWeight.Normal, "Regular"), (FontWeight.Bold, "Bold") })
                {
                    var platformFace = GetPlatformFace("Noto Sans KR", weight, FontStretch.Normal);
                    var expected = thin.WithVariations(null,
                        thin.NamedInstances.Single(i => i.Name == instanceName).Index);

                    Assert.Equal(weight, platformFace.Weight);
                    Assert.Equal(Advances(expected), Advances(platformFace));
                }
            }
        }

        [Win32Theory("Windows specific font")]
        [InlineData("Noto Sans KR", 100, 231.372)]
        [InlineData("Noto Sans KR", 400, 245.988)]
        [InlineData("Noto Sans KR", 700, 260.640)]
        [InlineData("Segoe UI Variable Text", 100, 216.352)]
        [InlineData("Segoe UI Variable Text", 400, 229.570)]
        [InlineData("Segoe UI Variable Text", 700, 248.625)]
        public void Should_Lay_Out_A_DirectWrite_Instance_At_The_Width_DirectWrite_Lays_It_Out(string familyName,
            int weight, double directWriteWidth)
        {
            using (Start())
            {
                GetPlatformFace(familyName, (FontWeight)weight, FontStretch.Normal);

                var typeface = new Typeface(new FontFamily(familyName), FontStyle.Normal, (FontWeight)weight);
                using var layout = new TextLayout("Thequickbrownfoxjumpsover", typeface, 18, Brushes.Black);

                Assert.Equal(directWriteWidth, layout.WidthIncludingTrailingWhitespace, 3);
            }
        }

        [Win32Fact("Windows specific font")]
        public void Should_Report_The_Weight_And_Stretch_Of_A_DirectWrite_Instance()
        {
            using (Start())
            {
                var condensedBold = GetPlatformFace("Bahnschrift", FontWeight.Bold, FontStretch.Condensed);

                Assert.Equal(FontWeight.Bold, condensedBold.Weight);
                Assert.Equal(FontStretch.Condensed, condensedBold.Stretch);

                var semiBold = GetPlatformFace("Bahnschrift", FontWeight.SemiBold, FontStretch.Normal);

                Assert.Equal(FontWeight.SemiBold, semiBold.Weight);
                Assert.Equal(FontStretch.Normal, semiBold.Stretch);
            }
        }

        [Win32Fact("Windows specific font")]
        public void Should_Get_The_Glyph_Outline_Of_A_DirectWrite_Instance()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                       .With(renderInterface: new PlatformRenderInterface(), fontManagerImpl: new FontManagerImpl())))
            {
                var bold = GetPlatformFace("Segoe UI Variable Text", FontWeight.Bold, FontStretch.Normal);
                var expected = GetNamedInstance("Segoe UI Variable Text", "Bold Text");

                Assert.True(bold.CharacterToGlyphMap.TryGetGlyph('H', out var glyph));

                Assert.Equal(expected.GetGlyphOutline(glyph)!.Bounds, bold.GetGlyphOutline(glyph)!.Bounds);
                Assert.NotEqual(GetRegular("Segoe UI Variable Text").GetGlyphOutline(glyph)!.Bounds,
                    bold.GetGlyphOutline(glyph)!.Bounds);
            }
        }

        [Win32Fact("Windows specific font")]
        public void Should_Return_A_DirectWrite_Instance_For_Its_Own_Named_Instance()
        {
            using (Start())
            {
                var bold = GetPlatformFace("Bahnschrift", FontWeight.Bold, FontStretch.Condensed);
                var index = bold.NamedInstances.Single(i => i.Name == "Bold Condensed").Index;

                Assert.Same(bold, bold.WithVariations(null, index));
            }
        }

        [Win32Fact("Windows specific font")]
        public void Should_Keep_The_Default_Instance_For_A_Regular_DirectWrite_Face()
        {
            using (Start())
            {
                var regular = GetPlatformFace("Bahnschrift", FontWeight.Normal, FontStretch.Normal);
                Assert.True(regular.VariationPosition.IsDefault);
                Assert.Equal(FontWeight.Normal, regular.Weight);
            }
        }

        [LinuxFact("Linux specific font")]
        public void Should_Shape_A_Fontconfig_Instance_With_The_Advances_Of_Its_Named_Instance()
        {
            using (Start())
            {
                AssertShapedLikeNamedInstance("Ubuntu", FontWeight.Bold, FontStretch.Normal, "Ubuntu", "Bold");
                AssertShapedLikeNamedInstance("Ubuntu", FontWeight.Light, FontStretch.Normal, "Ubuntu", "Light");
                AssertShapedLikeNamedInstance("Ubuntu Sans", FontWeight.Bold, FontStretch.Condensed, "Ubuntu Sans",
                    "Condensed Bold");
            }
        }

        [LinuxFact("Linux specific font")]
        public void Should_Report_The_Weight_And_Stretch_Of_A_Fontconfig_Instance()
        {
            using (Start())
            {
                var condensedBold = GetPlatformFace("Ubuntu Sans", FontWeight.Bold, FontStretch.Condensed);

                Assert.Equal(FontWeight.Bold, condensedBold.Weight);
                Assert.Equal(FontStretch.Condensed, condensedBold.Stretch);
            }
        }

        private static IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(fontManagerImpl: new FontManagerImpl()));
        }

        private static void AssertShapedLikeNamedInstance(string familyName, FontWeight weight, FontStretch stretch,
            string defaultFamilyName, string instanceName)
        {
            var platformFace = GetPlatformFace(familyName, weight, stretch);
            var expected = GetNamedInstance(defaultFamilyName, instanceName);

            Assert.Equal(Advances(expected), Advances(platformFace));
        }

        private static GlyphTypeface GetPlatformFace(string familyName, FontWeight weight, FontStretch stretch)
        {
            Assert.True(FontManager.Current.TryGetGlyphTypeface(
                new Typeface(familyName, FontStyle.Normal, weight, stretch), out var glyphTypeface));

            Assert.SkipWhen(glyphTypeface.VariationAxes.Count == 0, $"{familyName} is not installed as a variable font");

            return glyphTypeface;
        }

        private static GlyphTypeface GetRegular(string familyName)
            => GetPlatformFace(familyName, FontWeight.Normal, FontStretch.Normal);

        /// <summary>
        /// Gets a variation of a family's default instance at a named instance, read from the same
        /// tables as the platform's instance.
        /// </summary>
        /// <param name="defaultFamilyName">A family whose regular face is the font's default instance.</param>
        /// <param name="instanceName">The subfamily name of the named instance.</param>
        private static GlyphTypeface GetNamedInstance(string defaultFamilyName, string instanceName)
        {
            var defaultInstance = GetRegular(defaultFamilyName);

            Assert.True(defaultInstance.VariationPosition.IsDefault);

            return defaultInstance.WithVariations(null,
                defaultInstance.NamedInstances.Single(i => i.Name == instanceName).Index);
        }

        private static double[] Advances(GlyphTypeface glyphTypeface)
        {
            var buffer = TextShaper.Current.ShapeText(Text,
                new TextShaperOptions(glyphTypeface, glyphTypeface.Metrics.DesignEmHeight, 0,
                    CultureInfo.InvariantCulture));

            return Enumerable.Range(0, buffer.Length).Select(i => buffer[i].GlyphAdvance).ToArray();
        }
    }
}
