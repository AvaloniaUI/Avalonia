using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// FontManager is the seam where a Typeface's user-space FontVariations are applied
    /// to the resolved GlyphTypeface. These tests pin that application: the resolved
    /// typeface is bound to the normalized position, equal settings share one cached
    /// instance, and settings-free lookups stay at the default instance.
    /// </summary>
    public class FontManagerVariationTests
    {
        private const string InterVariableAsset =
            "resm:Avalonia.Base.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Base.UnitTests";

        private static readonly OpenTypeTag s_wghtTag = OpenTypeTag.Parse("wght");

        [Fact]
        public void TryGetGlyphTypeface_Applies_FontVariations()
        {
            using (Start())
            {
                var typeface = new Typeface("Inter Variable",
                    FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, FontVariationSettings.Parse("wght=700"));

                Assert.True(FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface));

                // wght=700 on Inter Variable normalizes to 0.54 (through avar) — the same
                // value CreateNormalizedPosition produces, proving the settings reached
                // the variation seam and were not dropped during resolution.
                Assert.True(glyphTypeface.VariationPosition.TryGetCoordinate(s_wghtTag, out var wght));
                Assert.Equal(0.54f, wght, precision: 4);
            }
        }

        [Fact]
        public void TryGetGlyphTypeface_Without_Variations_Resolves_Default_Instance()
        {
            using (Start())
            {
                var typeface = new Typeface("Inter Variable");

                Assert.True(FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface));

                Assert.True(glyphTypeface.VariationPosition.IsDefault);
            }
        }

        [Fact]
        public void Equal_Variations_Resolve_To_The_Same_Instance()
        {
            using (Start())
            {
                var a = new Typeface("Inter Variable",
                    FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, FontVariationSettings.Parse("wght=700"));
                var b = new Typeface("Inter Variable",
                    FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, FontVariationSettings.Parse("wght=700"));

                Assert.True(FontManager.Current.TryGetGlyphTypeface(a, out var first));
                Assert.True(FontManager.Current.TryGetGlyphTypeface(b, out var second));

                // The base typeface is cached by the font collection and the varied clone
                // is cached on the base, so equal settings must not allocate per lookup.
                Assert.Same(first, second);

                Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("Inter Variable"), out var plain));
                Assert.NotSame(plain, first);
            }
        }

        private static IDisposable Start() =>
            UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(fontManagerImpl: new VariableFontManagerStub()));

        /// <summary>
        /// Serves the embedded Inter Variable font for every family request, so the
        /// resolution pipeline (FontManager → SystemFontCollection → platform impl) runs
        /// for real against a variable font without depending on system fonts.
        /// </summary>
        private class VariableFontManagerStub : IFontManagerImpl
        {
            public string GetDefaultFontFamilyName() => "Inter Variable";

            public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
                new[] { "Inter Variable" };

            public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
                FontStretch fontStretch, string? familyName, CultureInfo? culture,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                platformTypeface = null;
                return false;
            }

            public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
                FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                var assetLoader = new StandardAssetLoader();
                using var stream = assetLoader.Open(new Uri(InterVariableAsset));
                platformTypeface = new CustomPlatformTypeface(stream, familyName);
                return true;
            }

            public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                platformTypeface = new CustomPlatformTypeface(stream);
                return true;
            }

            public bool TryGetFamilyTypefaces(string familyName,
                [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
            {
                familyTypefaces = null;
                return false;
            }
        }
    }
}
