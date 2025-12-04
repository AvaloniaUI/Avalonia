#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class FontCollectionTests
    {
        private const string NotoMono =
          "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests";

        [InlineData("Hello World 6", "Hello World 6", FontStyle.Normal, FontWeight.Normal)]
        [InlineData("Hello World Italic", "Hello World", FontStyle.Italic, FontWeight.Normal)]
        [InlineData("Hello World Italic Bold", "Hello World", FontStyle.Italic, FontWeight.Bold)]
        [InlineData("FontAwesome 6 Free Regular", "FontAwesome 6 Free", FontStyle.Normal, FontWeight.Normal)]
        [InlineData("FontAwesome 6 Free Solid", "FontAwesome 6 Free", FontStyle.Normal, FontWeight.Solid)]
        [InlineData("FontAwesome 6 Brands", "FontAwesome 6 Brands", FontStyle.Normal, FontWeight.Normal)]
        [Theory]
        public void Should_Get_Implicit_Typeface(string input, string familyName, FontStyle style, FontWeight weight)
        {
            var typeface = new Typeface(input);

            var result = FontCollectionBase.GetImplicitTypeface(typeface, out var normalizedFamilyName);

            Assert.Equal(familyName, normalizedFamilyName);
            Assert.Equal(style, result.Style);
            Assert.Equal(weight, result.Weight);
            Assert.Equal(FontStretch.Normal, result.Stretch);
        }

        [Win32Fact("Relies on some installed font family")]
        public void Should_Cache_Nearest_Match()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontManager = FontManager.Current;

                var fontCollection = new TestSystemFontCollection(FontManager.Current);

                Assert.True(fontCollection.TryGetGlyphTypeface("Arial", FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal, out var glyphTypeface));

                Assert.True(fontCollection.GlyphTypefaceCache.TryGetValue("Arial", out var glyphTypefaces));

                Assert.Equal(2, glyphTypefaces.Count);

                Assert.True(glyphTypefaces.ContainsKey(new FontCollectionKey(FontStyle.Normal, FontWeight.Black, FontStretch.Normal)));

                fontCollection.TryGetGlyphTypeface("Arial", FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal, out var otherGlyphTypeface);

                Assert.Equal(glyphTypeface, otherGlyphTypeface);
            }
        }

        private class TestSystemFontCollection : SystemFontCollection
        {
            public TestSystemFontCollection(FontManager fontManager) : base(fontManager)
            {
                
            }

            public IDictionary<string, ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>> GlyphTypefaceCache => _glyphTypefaceCache;
        }

        [Fact]
        public void Should_Use_Fallback()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
            {
                var source = new Uri(NotoMono, UriKind.Absolute);

                var fallback = new FontFallback { FontFamily = new FontFamily("Arial"), UnicodeRange = new UnicodeRange('A', 'A') };

                var fontCollection = new CustomizableFontCollection(source, source, new[] { fallback  });

                fontCollection.Initialize(FontManager.Current.PlatformImpl);

                Assert.True(fontCollection.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var match));

                Assert.Equal("Arial", match.FontFamily.Name);
            }
        }

        [Fact]
        public void Should_Ignore_FontFamily()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
            {
                var key = new Uri(NotoMono, UriKind.Absolute);

                var ignorable = new FontFamily(new Uri(NotoMono, UriKind.Absolute), "Noto Mono");

                var fontCollection = new CustomizableFontCollection(key, key, null, new[] { ignorable });

                fontCollection.Initialize(FontManager.Current.PlatformImpl);

                var typeface = new Typeface(ignorable);

                var glyphTypeface = typeface.GlyphTypeface;

                Assert.False(fontCollection.TryCreateSyntheticGlyphTypeface(
                    typeface.GlyphTypeface,
                    FontStyle.Italic,
                    FontWeight.DemiBold,
                    FontStretch.Normal,
                    out var syntheticGlyphTypeface));
            }
        }

        [Fact]
        public void SystemFontCollection_Only_Calls_Platform_TryMatchCharacter_Once_On_Success()
        {
            var countingImpl = new CustomFontManagerImpl();

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: countingImpl)))
            {
                var fontManager = FontManager.Current;

                var systemFonts = fontManager.SystemFonts as SystemFontCollection;

                Assert.NotNull(systemFonts);

                // First call should invoke platform TryMatchCharacter and populate cache
                Assert.True(systemFonts.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var match1));

                // Second call should be served from cache and should not call platform TryMatchCharacter again
                Assert.True(systemFonts.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var match2));

                Assert.Equal(1, countingImpl.TryMatchCharacterCount);

                Assert.Equal(match1.FontFamily.Name, match2.FontFamily.Name);
            }
        }

        [Fact]
        public void Should_Cache_Font_By_Normalized_Name_When_Platform_Returns_Regular_Suffix()
        {
            var impl = new RegularSuffixFontManagerImpl("Default");

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: impl)))
            {
                var fontManager = FontManager.Current;

                var systemFonts = new TestSystemFontCollection(fontManager);

                Assert.NotNull(systemFonts);

                // Call TryMatchCharacter which should invoke platform TryMatchCharacter and add to cache
                Assert.True(systemFonts.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var match));

                var normalized = fontManager.DefaultFontFamily.Name;

                // Ensure the cache contains the normalized name (without 'Regular')
                Assert.True(systemFonts.GlyphTypefaceCache.ContainsKey(normalized));

                // Ensure the raw returned name with ' Regular' is not used as cache key
                Assert.False(systemFonts.GlyphTypefaceCache.ContainsKey(normalized + " Regular"));

                Assert.True(systemFonts.TryGetGlyphTypeface(normalized + " Regular", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var cachedGlyphTypeface));

                Assert.Equal(match.FontFamily.Name, cachedGlyphTypeface.FamilyName);
            }
        }

        private class CustomizableFontCollection : EmbeddedFontCollection
        {
            private readonly IReadOnlyList<FontFallback>? _fallbacks;
            private readonly IReadOnlyList<FontFamily>? _ignorables;

            public CustomizableFontCollection(Uri key, Uri source, IReadOnlyList<FontFallback>? fallbacks = null, IReadOnlyList<FontFamily>? ignorables = null) : base(key, source)
            {
                _fallbacks = fallbacks;
                _ignorables = ignorables;
            }

            public override bool TryMatchCharacter(
                int codepoint, 
                FontStyle style, 
                FontWeight weight, 
                FontStretch stretch, 
                string? familyName, 
                CultureInfo? culture, 
                out Typeface match)
            {
                if(_fallbacks is not null)
                {
                    foreach (var fallback in _fallbacks)
                    {
                        if (fallback.UnicodeRange.IsInRange(codepoint))
                        {
                            match = new Typeface(fallback.FontFamily, style, weight, stretch);

                            return true;
                        }
                    }
                }

                return base.TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, out match);
            }

            public override bool TryCreateSyntheticGlyphTypeface(
                IGlyphTypeface glyphTypeface, 
                FontStyle style, 
                FontWeight weight,
                FontStretch stretch, 
                [NotNullWhen(true)] out IGlyphTypeface? syntheticGlyphTypeface)
            {
                syntheticGlyphTypeface = null;

                if(_ignorables is not null)
                {
                    foreach (var ignorable in _ignorables)
                    {
                        if (glyphTypeface.FamilyName == ignorable.Name || glyphTypeface is IGlyphTypeface2 glyphTypeface2 && glyphTypeface2.TypographicFamilyName == ignorable.Name)
                        {
                            return false;
                        }
                    }
                }

                return base.TryCreateSyntheticGlyphTypeface(glyphTypeface, style, weight, stretch, out syntheticGlyphTypeface);
            }
        }

        private class RegularSuffixFontManagerImpl : IFontManagerImpl2
        {
            private readonly string _defaultFamilyName;

            public RegularSuffixFontManagerImpl(string defaultFamilyName)
            {
                _defaultFamilyName = defaultFamilyName;
            }

            public int TryMatchCharacterCount { get; private set; }

            public string GetDefaultFontFamilyName() => _defaultFamilyName;

            public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false) => new[] { _defaultFamilyName };

            public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, CultureInfo? culture, out Typeface typeface)
            {
                if (TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, culture, out IGlyphTypeface? glyphTypeface))
                {
                    typeface = new Typeface(glyphTypeface.FamilyName, fontStyle, fontWeight);

                    return true;
                }

                typeface = default;

                return false;
            }

            public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, CultureInfo? culture, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
            {
                TryMatchCharacterCount++;

                // Return a glyph typeface with ' Regular' appended so it will be normalized
                glyphTypeface = new SimpleGlyphTypeface(_defaultFamilyName + " Regular", fontStyle, fontWeight, fontStretch);

                return true;
            }

            public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
            {
                glyphTypeface = null;
                return false;
            }

            public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
            {
                glyphTypeface = null;
                return false;
            }

            public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
            {
                familyTypefaces = null;

                return false;
            }
        }

        // Minimal IGlyphTypeface implementation for testing
        private class SimpleGlyphTypeface : IGlyphTypeface
        {
            public SimpleGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch)
            {
                FamilyName = familyName;
                Style = style;
                Weight = weight;
                Stretch = stretch;
            }

            public FontMetrics Metrics => new FontMetrics { DesignEmHeight = 10, Ascent = 5, Descent = 3, LineGap = 0, IsFixedPitch = false };

            public int GlyphCount => 1;

            public FontSimulations FontSimulations => FontSimulations.None;

            public string FamilyName { get; }

            public FontWeight Weight { get; }

            public FontStyle Style { get; }

            public FontStretch Stretch { get; }

            public void Dispose() { }

            public ushort GetGlyph(uint codepoint) => 1;

            public bool TryGetGlyph(uint codepoint, out ushort glyph)
            {
                glyph = 1;
                return true;
            }

            public int GetGlyphAdvance(ushort glyph) => 1;

            public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
            {
                var arr = new int[glyphs.Length];
                for (var i = 0; i < arr.Length; i++) arr[i] = 1;
                return arr;
            }

            public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
            {
                var arr = new ushort[codepoints.Length];
                for (var i = 0; i < arr.Length; i++) arr[i] = 1;
                return arr;
            }

            public bool TryGetTable(uint tag, out byte[] table)
            {
                table = null!;
                return false;
            }

            public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
            {
                metrics = new GlyphMetrics { Width = 1, Height = 1 };
                return true;
            }
        }
    }
}
