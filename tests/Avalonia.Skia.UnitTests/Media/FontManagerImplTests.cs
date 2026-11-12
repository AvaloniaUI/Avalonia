#nullable enable

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;
using static Avalonia.Media.Fonts.Tables.OS2Table;

namespace Avalonia.Skia.UnitTests.Media
{
    public class FontManagerImplTests
    {
        private const FontSelectionFlags Regular = FontSelectionFlags.REGULAR;

        [Fact]
        public void Should_Report_A_Platform_Bold_For_A_Static_Regular_Face_Reported_Bold()
        {
            Assert.Equal(FontSimulations.Bold, FontManagerImpl.GetPlatformSimulations(700, SKFontStyleSlant.Upright,
                400, Regular, isVariable: false, hasSlantAxis: false));
        }

        [Fact]
        public void Should_Report_A_Platform_Oblique_For_A_Static_Upright_Face_Reported_Slanted()
        {
            Assert.Equal(FontSimulations.Oblique, FontManagerImpl.GetPlatformSimulations(400,
                SKFontStyleSlant.Oblique, 400, Regular, isVariable: false, hasSlantAxis: false));
        }

        [Theory]
        [InlineData(700, 700, (int)FontSelectionFlags.BOLD)]
        [InlineData(700, 7, (int)FontSelectionFlags.REGULAR)]
        [InlineData(600, 600, (int)FontSelectionFlags.REGULAR)]
        public void Should_Not_Report_A_Platform_Bold_For_A_Static_Face_Designed_Bold(int reportedWeight,
            int weightClass, int fsSelection)
        {
            Assert.Equal(FontSimulations.None, FontManagerImpl.GetPlatformSimulations(reportedWeight,
                SKFontStyleSlant.Upright, weightClass, (FontSelectionFlags)fsSelection, isVariable: false, hasSlantAxis: false));
        }

        [Theory]
        [InlineData(SKFontStyleSlant.Italic, (int)FontSelectionFlags.ITALIC)]
        [InlineData(SKFontStyleSlant.Oblique, (int)FontSelectionFlags.OBLIQUE)]
        public void Should_Not_Report_A_Platform_Oblique_For_A_Static_Face_Designed_Slanted(SKFontStyleSlant slant,
            int fsSelection)
        {
            Assert.Equal(FontSimulations.None, FontManagerImpl.GetPlatformSimulations(400, slant, 400, (FontSelectionFlags)fsSelection,
                isVariable: false, hasSlantAxis: false));
        }

        [Theory]
        [InlineData(600)]
        [InlineData(700)]
        [InlineData(900)]
        public void Should_Not_Report_A_Platform_Bold_For_A_Variable_Face_Instance(int reportedWeight)
        {
            // The OS/2 table of a variable font describes its default instance only.
            Assert.Equal(FontSimulations.None, FontManagerImpl.GetPlatformSimulations(reportedWeight,
                SKFontStyleSlant.Upright, 400, Regular, isVariable: true, hasSlantAxis: false));
        }

        [Fact]
        public void Should_Report_A_Platform_Oblique_For_A_Variable_Face_Without_A_Slant_Axis()
        {
            Assert.Equal(FontSimulations.Oblique, FontManagerImpl.GetPlatformSimulations(700,
                SKFontStyleSlant.Oblique, 400, Regular, isVariable: true, hasSlantAxis: false));
        }

        [Theory]
        [InlineData(SKFontStyleSlant.Italic)]
        [InlineData(SKFontStyleSlant.Oblique)]
        public void Should_Not_Report_A_Platform_Oblique_For_A_Variable_Face_With_A_Slant_Axis(
            SKFontStyleSlant slant)
        {
            Assert.Equal(FontSimulations.None, FontManagerImpl.GetPlatformSimulations(400, slant, 400, Regular,
                isVariable: true, hasSlantAxis: true));
        }

        [Theory]
        [InlineData(FontSimulations.Bold, (int)SKFontStyleWeight.Normal)]
        [InlineData(FontSimulations.Oblique, (int)SKFontStyleWeight.Bold)]
        [InlineData(FontSimulations.Bold | FontSimulations.Oblique, (int)SKFontStyleWeight.Normal)]
        public void Should_Match_The_Designed_Face_Of_A_Colour_Font_The_Platform_Simulated(
            FontSimulations platformSimulations, int expectedWeight)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var matched = LoadTypeface(ArefBoldUri);
                var familyName = matched.FamilyName;
                var width = matched.FontWidth;
                var requests = new List<(string FamilyName, SKFontStyle Style)>();
                SKTypeface? designed = null;

                // A weight the platform did not simulate is the face's own and is asked for again.
                var result = FontManagerImpl.MatchDesignedColourFace(matched, platformSimulations, (name, fontStyle) =>
                    {
                        requests.Add((name, fontStyle));

                        return designed = LoadTypeface(ArefRegularUri);
                    });

                var request = Assert.Single(requests);

                Assert.Equal(familyName, request.FamilyName);
                Assert.Equal(expectedWeight, request.Style.Weight);
                Assert.Equal(SKFontStyleSlant.Upright, request.Style.Slant);
                Assert.Equal(width, request.Style.Width);

                Assert.Same(designed, result);

                // The first match is replaced, so nothing else releases its native typeface.
                Assert.Equal(IntPtr.Zero, matched.Handle);

                result.Dispose();
            }
        }

        [Fact]
        public void Should_Keep_A_Colour_Font_Face_The_Platform_Did_Not_Simulate()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                using var matched = LoadTypeface(ArefBoldUri);

                var result = FontManagerImpl.MatchDesignedColourFace(matched, FontSimulations.None,
                    (_, _) => throw new InvalidOperationException("Unexpected platform match."));

                Assert.Same(matched, result);
            }
        }

        [Theory]
        [InlineData(FontStyle.Normal)]
        [InlineData(FontStyle.Italic)]
        public void Should_Keep_The_Designed_Bold_Face_Of_A_Colour_Font_Family(FontStyle style)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var bold = LoadTypeface(ArefBoldUri);

                using var typeface = FontManagerImpl.CreatePlatformTypeface(bold, style, FontWeight.Bold,
                    FontStretch.Normal, (_, _) => throw new InvalidOperationException("Unexpected platform match."));

                Assert.Same(bold, typeface.SKTypeface);
                Assert.Equal(FontWeight.Bold, typeface.Weight);
                Assert.Equal(FontSimulations.None, typeface.FontSimulations & FontSimulations.Bold);
            }
        }

        [Theory]
        [InlineData(FontStyle.Normal, 700, FontSimulations.Bold)]
        [InlineData(FontStyle.Italic, 400, FontSimulations.Oblique)]
        [InlineData(FontStyle.Italic, 700, FontSimulations.Bold | FontSimulations.Oblique)]
        public void Should_Simulate_A_Colour_Font_Face_Matched_To_A_Bold_Or_Slanted_Request(FontStyle style,
            int weight, FontSimulations expectedSimulations)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var regular = LoadTypeface(ArefRegularUri);

                using var typeface = FontManagerImpl.CreatePlatformTypeface(regular, style, (FontWeight)weight,
                    FontStretch.Normal, (_, _) => throw new InvalidOperationException("Unexpected platform match."));

                Assert.Same(regular, typeface.SKTypeface);
                Assert.Equal(expectedSimulations, typeface.FontSimulations);
                Assert.Equal((FontWeight)weight, typeface.Weight);
                Assert.Equal(style, typeface.Style);
            }
        }

        [Theory]
        [InlineData(FontSimulations.Bold)]
        [InlineData(FontSimulations.Oblique)]
        [InlineData(FontSimulations.Bold | FontSimulations.Oblique)]
        public void Should_Simulate_A_Colour_Font_Face_Created_From_A_Stream(FontSimulations simulations)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

                using var stream = assetLoader.Open(new Uri(ArefRegularUri, UriKind.Absolute));

                Assert.True(new FontManagerImpl().TryCreateGlyphTypeface(stream, simulations, out var typeface));

                using (typeface)
                {
                    Assert.Equal(simulations, typeface.FontSimulations);
                }
            }
        }

        private const string ArefRegularUri =
            "resm:Avalonia.Skia.UnitTests.Fonts.ArefRuqaaInk-Regular.Subset.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string ArefBoldUri =
            "resm:Avalonia.Skia.UnitTests.Fonts.ArefRuqaaInk-Bold.Subset.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string EmojiUri =
            "resm:Avalonia.Skia.UnitTests.Assets.TwitterColorEmoji-SVGinOT.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string NotoMonoUri =
            "resm:Avalonia.Skia.UnitTests.Assets.NotoMono-Regular.ttf?assembly=Avalonia.Skia.UnitTests";

        private static SKTypeface LoadTypeface(string uri)
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            using var stream = assetLoader.Open(new Uri(uri, UriKind.Absolute));
            using var data = SKData.Create(stream);

            return SKTypeface.FromData(data) ??
                   throw new InvalidOperationException($"Could not load the test font {uri}.");
        }
    }
}
