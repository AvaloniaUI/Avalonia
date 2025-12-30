#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

        [Win32Fact("Relies on some installed font family")]
        public void Should_Cache_Nearest_Match()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontCollection = new TestSystemFontCollection(FontManager.Current.PlatformImpl);

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
            public TestSystemFontCollection(IFontManagerImpl platformImpl) : base(platformImpl)
            {
            }

            public IDictionary<string, ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>> GlyphTypefaceCache => _glyphTypefaceCache;
        }

        /// <summary>
        /// Verifies no leak when the returned typeface's FamilyName differs from requested.
        /// This happens with fonts like "Segoe UI Variable Text" returning FamilyName="Segoe UI Variable".
        /// </summary>
        [Fact]
        public void Should_Not_Leak_When_Typeface_FamilyName_Differs_From_Requested()
        {
            var fontManager = new MockFontManagerImpl(returnDifferentFamilyName: true);
            
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);
                
                // First request creates the typeface
                fontCollection.TryGetGlyphTypeface("TestFont", FontStyle.Normal, FontWeight.Bold, FontStretch.Normal, out _);
                var countAfterFirst = fontManager.CreateCount;
                
                // Subsequent requests should hit cache, not create new typefaces
                for (int i = 0; i < 100; i++)
                {
                    fontCollection.TryGetGlyphTypeface("TestFont", FontStyle.Normal, FontWeight.Bold, FontStretch.Normal, out _);
                }
                
                Assert.Equal(countAfterFirst, fontManager.CreateCount);
            }
        }

        private class MockFontManagerImpl : IFontManagerImpl
        {
            public int CreateCount { get; private set; }
            private readonly bool _returnDifferentFamilyName;
            
            public MockFontManagerImpl(bool returnDifferentFamilyName) => _returnDifferentFamilyName = returnDifferentFamilyName;
            
            public string GetDefaultFontFamilyName() => "TestFont";
            public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false) => new[] { "TestFont" };
            
            public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, 
                FontStretch fontStretch, string? familyName, CultureInfo? culture, out Typeface typeface)
            {
                typeface = new Typeface("TestFont");
                return true;
            }
            
            public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
                FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
            {
                CreateCount++;
                var returnedName = _returnDifferentFamilyName ? familyName + " UI" : familyName;
                glyphTypeface = new MockGlyphTypeface(returnedName, style, weight, stretch);
                return true;
            }
            
            public bool TryCreateGlyphTypeface(System.IO.Stream stream, FontSimulations fontSimulations, 
                [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
            {
                glyphTypeface = new MockGlyphTypeface("TestFont", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);
                return true;
            }
        }
        
        private class MockGlyphTypeface : IGlyphTypeface
        {
            public MockGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch)
            {
                FamilyName = familyName;
                Style = style;
                Weight = weight;
                Stretch = stretch;
            }
            
            public string FamilyName { get; }
            public FontStyle Style { get; }
            public FontWeight Weight { get; }
            public FontStretch Stretch { get; }
            public int GlyphCount => 1;
            public FontMetrics Metrics => new FontMetrics { DesignEmHeight = 1000, Ascent = -800, Descent = 200 };
            public FontSimulations FontSimulations => FontSimulations.None;
            
            public ushort GetGlyph(uint codepoint) => 1;
            public bool TryGetGlyph(uint codepoint, out ushort glyph) { glyph = 1; return true; }
            public int GetGlyphAdvance(ushort glyph) => 500;
            public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs) => new int[glyphs.Length];
            public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints) => new ushort[codepoints.Length];
            public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics) { metrics = default; return true; }
            public bool TryGetTable(uint tag, out byte[] table) { table = Array.Empty<byte>(); return false; }
            public void Dispose() { }
        }

        [Fact]
        public void Should_Use_Fallback()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
            {
                var source = new Uri(NotoMono, UriKind.Absolute);

                var fallback = new FontFallback { FontFamily = new FontFamily("Arial"), UnicodeRange = new UnicodeRange('A', 'A') };

                var fontCollection = new CustomizableFontCollection(source, source, new[] { fallback  });

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
    }
}
