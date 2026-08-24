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

            public IDictionary<string, ConcurrentDictionary<FontCollectionKey, GlyphTypeface?>> GlyphTypefaceCache => _glyphTypefaceCache;
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
                GlyphTypeface glyphTypeface, 
                FontStyle style, 
                FontWeight weight,
                FontStretch stretch, 
                [NotNullWhen(true)] out GlyphTypeface? syntheticGlyphTypeface)
            {
                syntheticGlyphTypeface = null;

                if(_ignorables is not null)
                {
                    foreach (var ignorable in _ignorables)
                    {
                        if (glyphTypeface.FamilyName == ignorable.Name || glyphTypeface.TypographicFamilyName == ignorable.Name)
                        {
                            return false;
                        }
                    }
                }

                return base.TryCreateSyntheticGlyphTypeface(glyphTypeface, style, weight, stretch, out syntheticGlyphTypeface);
            }
        }

        [Fact]
        public void Should_Cache_Synthetic_Match_Under_Requested_Family_Name()
        {
            var fontManager = new AliasFontManagerImpl(alias: "MyAlias");

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);
                var blackKey = new FontCollectionKey(FontStyle.Normal, FontWeight.Black, FontStretch.Normal);

                // Prime the cache with the bare family, as any control asking for the alias at a
                // normal weight would. This is what makes the next lookup take the nearest match.
                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "MyAlias", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "MyAlias", FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var first));

                // Guards the test itself: the first resolution must really be a synthesised bold.
                // If the backing font could not be emboldened, TryCreateSyntheticGlyphTypeface would
                // fail and the old else branch would cache the nearest match, making everything below
                // pass against unfixed code.
                Assert.Equal(FontSimulations.Bold, first.FontSimulations);

                var creationsAfterFirstCall = fontManager.StreamTypefaceCreations;

                for (var i = 0; i < 10; i++)
                {
                    Assert.True(fontCollection.TryGetGlyphTypeface(
                        "MyAlias", FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var next));

                    Assert.Same(first, next);
                }

                // Each synthesis copies the entire font file through IPlatformTypeface.TryGetStream,
                // so an uncached synthetic means one full font copy per call.
                Assert.Equal(creationsAfterFirstCall, fontManager.StreamTypefaceCreations);

                Assert.True(fontCollection.GlyphTypefaceCache.TryGetValue("MyAlias", out var cached));
                Assert.True(cached.ContainsKey(blackKey));
            }
        }

        /// <summary>
        /// Font manager whose <c>MyAlias</c> family resolves through the platform but is absent from
        /// the installed family list — the shape of a platform alias (for instance Android's
        /// <c>&lt;alias name="arial" to="sans-serif"/&gt;</c> in <c>/system/etc/fonts.xml</c>).
        /// Such a family cannot be found again by the family-name search, so nothing repairs a
        /// missing cache entry.
        ///
        /// The alias is backed by an embedded test font rather than an installed one, so the test
        /// runs identically on every platform.
        /// </summary>
        private sealed class AliasFontManagerImpl : IFontManagerImpl
        {
            /// <summary>Named explicitly rather than enumerated: the backing font must be a real
            /// text face, since a font that cannot be emboldened would make the test pass against
            /// unfixed code (the old else branch cached the nearest match).</summary>
            private const string BackingFontUri =
                "resm:Avalonia.Skia.UnitTests.Assets.NotoMono-Regular.ttf?assembly=Avalonia.Skia.UnitTests";

            private readonly IFontManagerImpl _inner = new FontManagerImpl();
            private readonly string _alias;

            public AliasFontManagerImpl(string alias)
            {
                _alias = alias;
            }

            /// <summary>Number of typefaces created from a stream, i.e. of synthetic emboldenings.</summary>
            public int StreamTypefaceCreations { get; private set; }

            public string GetDefaultFontFamilyName() => _inner.GetDefaultFontFamilyName();

            public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
                => Array.Empty<string>();

            public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
                FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                // The alias always resolves to the regular face of the backing font, never to the
                // requested weight — exactly what a platform alias does.
                if (string.Equals(familyName, _alias, StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = OpenBackingFont();

                    return _inner.TryCreateGlyphTypeface(stream, FontSimulations.None, out platformTypeface);
                }

                platformTypeface = null;

                return false;
            }

            private static Stream OpenBackingFont()
            {
                var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

                return assetLoader.Open(new Uri(BackingFontUri, UriKind.Absolute));
            }

            public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                StreamTypefaceCreations++;

                return _inner.TryCreateGlyphTypeface(stream, fontSimulations, out platformTypeface);
            }

            public bool TryGetFamilyTypefaces(string familyName,
                [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
                => _inner.TryGetFamilyTypefaces(familyName, out familyTypefaces);

            public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
                FontStretch fontStretch, string? familyName, CultureInfo? culture,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
                => _inner.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName,
                    culture, out platformTypeface);
        }
    }
}
