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
using SkiaSharp;
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

        [Fact]
        public void Should_Ignore_Family_Name_Casing_When_Resolving_A_Synthetic_Match()
        {
            var fontManager = new AliasFontManagerImpl(alias: "MyAlias");

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "MyAlias", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));

                // Casing must not decide whether a request gets a synthesised bold. A cache keyed
                // ordinally sends this lookup past the synthesis branch and down the family-name
                // search, which returns the nearest match raw - so the very same family renders
                // faux-bold under one casing and regular weight under another.
                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "MYALIAS", FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var upperCase));

                Assert.Equal(FontSimulations.Bold, upperCase.FontSimulations);

                var creationsAfterFirstCall = fontManager.StreamTypefaceCreations;

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "MyAlias", FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var mixedCase));

                // One shared cache entry, so the other casing neither re-synthesises nor gets a
                // second instance of the same face.
                Assert.Same(upperCase, mixedCase);
                Assert.Equal(creationsAfterFirstCall, fontManager.StreamTypefaceCreations);
            }
        }

        [Fact]
        public void Should_Not_Cache_A_Family_Twice_When_The_Platform_Returns_Another_Casing()
        {
            // The platform reports the family as "Noto Mono"; the caller asks in lower case, as any
            // XAML author may.
            var fontManager = new AliasFontManagerImpl(alias: "Noto Mono");

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "noto mono", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));

                // A cache keyed ordinally stores the requested casing beside the platform's own, but
                // AddFontFamily de-duplicates case-insensitively and publishes only the first of the
                // two, leaving the second bucket unreachable from every family-name search.
                Assert.Single(fontCollection.GlyphTypefaceCache);
                Assert.Equal(fontCollection.GlyphTypefaceCache.Count, fontCollection.Count);
            }
        }

        [Fact]
        public void Should_Reuse_An_Already_Cached_Synthetic_Glyph_Typeface()
        {
            var fontManager = new AliasFontManagerImpl(alias: "MyAlias");

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "MyAlias", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var regular));

                Assert.True(fontCollection.TryCreateSyntheticGlyphTypeface(
                    regular, FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var first));

                Assert.Equal(FontSimulations.Bold, first.FontSimulations);

                var creationsAfterFirstCall = fontManager.StreamTypefaceCreations;

                Assert.True(fontCollection.TryCreateSyntheticGlyphTypeface(
                    regular, FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var second));

                // A second synthesis builds a GlyphTypeface that then loses the cache slot to the
                // first one, so it is returned to the caller but never cached and never disposed -
                // and GlyphTypeface has no finalizer, so its native typeface is retained until the
                // process exits.
                Assert.Same(first, second);
                Assert.Equal(creationsAfterFirstCall, fontManager.StreamTypefaceCreations);
            }
        }

        /// <summary>
        /// Font manager whose <c>MyAlias</c> family resolves through the platform but is absent from
        /// the installed family list, the shape of a platform alias (for instance Android's
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

            /// <summary>Number of typefaces created from a stream: both the alias resolution and every
            /// synthetic emboldening go through this overload, so the counter also proves that a cached
            /// result short-circuits the platform call.</summary>
            public int StreamTypefaceCreations { get; private set; }

            public string GetDefaultFontFamilyName() => _inner.GetDefaultFontFamilyName();

            public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
                => Array.Empty<string>();

            public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
                FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                // The alias always resolves to the regular face of the backing font, never to the
                // requested weight, exactly what a platform alias does.
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

        [Theory]
        [InlineData(FontStyle.Normal, 700, FontSimulations.Bold)]
        [InlineData(FontStyle.Normal, 800, FontSimulations.Bold)]
        [InlineData(FontStyle.Italic, 400, FontSimulations.Oblique)]
        [InlineData(FontStyle.Italic, 700, FontSimulations.Bold | FontSimulations.Oblique)]
        public void Should_Use_The_Platform_Simulation_When_The_Family_Lacks_The_Face(FontStyle style, int weight,
            FontSimulations expectedSimulations)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                // A cached regular face is what makes a mismatching platform answer take the nearest match.
                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", style, (FontWeight)weight, FontStretch.Normal, out var glyphTypeface));

                // Synthesizing from a stream copies the whole font file and keeps the copy alive in the cache.
                Assert.Equal(0, fontManager.StreamTypefaceCreations);

                Assert.Equal(expectedSimulations, glyphTypeface.FontSimulations);
                Assert.Equal(expectedSimulations, glyphTypeface.PlatformTypeface.FontSimulations);
                Assert.Equal((FontWeight)weight, glyphTypeface.Weight);
                Assert.Equal(style, glyphTypeface.Style);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Keep_Regular_And_Simulated_Bold_Apart_In_Either_Order(bool boldFirst)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                GlyphTypeface? bold = null;
                GlyphTypeface? regular = null;

                foreach (var requestBold in boldFirst ? new[] { true, false } : new[] { false, true })
                {
                    Assert.True(fontCollection.TryGetGlyphTypeface("Noto Mono", FontStyle.Normal,
                        requestBold ? FontWeight.Bold : FontWeight.Normal, FontStretch.Normal, out var glyphTypeface));

                    if (requestBold)
                    {
                        bold = glyphTypeface;
                    }
                    else
                    {
                        regular = glyphTypeface;
                    }
                }

                Assert.NotNull(bold);
                Assert.NotNull(regular);
                Assert.NotSame(bold, regular);

                Assert.Equal(FontSimulations.Bold, bold.FontSimulations);
                Assert.Equal(FontWeight.Bold, bold.Weight);

                Assert.Equal(FontSimulations.None, regular.FontSimulations);
                Assert.Equal(FontWeight.Normal, regular.Weight);

                Assert.Equal(0, fontManager.StreamTypefaceCreations);
            }
        }

        [Fact]
        public void Should_Keep_The_Platform_Simulation_For_A_Character_Fallback()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Bold, FontStretch.Normal,
                    null, null, out var match));

                Assert.True(fontCollection.TryGetGlyphTypeface(match.FontFamily.Name, match.Style, match.Weight,
                    match.Stretch, out var glyphTypeface));

                Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
                Assert.Equal(FontSimulations.Bold, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.Bold, glyphTypeface.Weight);
                Assert.Equal(0, fontManager.StreamTypefaceCreations);
            }
        }

        [Theory]
        [InlineData(FontStyle.Normal, FontSimulations.None)]
        [InlineData(FontStyle.Italic, FontSimulations.Oblique)]
        public void Should_Use_The_Real_Bold_Face_When_The_Family_Has_One(FontStyle style,
            FontSimulations expectedSimulations)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(InterRegularUri, InterBoldUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", style, FontWeight.Bold, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);

                // Only the slant is simulated: the outlines come from the bold face itself.
                Assert.Equal(expectedSimulations, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.Bold, glyphTypeface.Weight);
                Assert.Equal(style, glyphTypeface.Style);
            }
        }

        private const string NotoMonoRegularUri =
            "resm:Avalonia.Skia.UnitTests.Assets.NotoMono-Regular.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string InterRegularUri =
            "resm:Avalonia.Skia.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string InterBoldUri =
            "resm:Avalonia.Skia.UnitTests.Assets.Inter-Bold.ttf?assembly=Avalonia.Skia.UnitTests";

        /// <summary>
        /// Font manager with a single installed family backed by embedded fonts, the shape of a
        /// platform font manager that matches a missing face to the nearest one it has and simulates
        /// the difference (CoreText and fontconfig do this for a family without a bold or italic
        /// face). Only the platform match itself is replaced: the matched <see cref="SKTypeface"/> is
        /// wrapped the way <see cref="FontManagerImpl"/> wraps the typeface its platform matched.
        /// </summary>
        private sealed class InstalledFamilyFontManagerImpl : IFontManagerImpl
        {
            private readonly IFontManagerImpl _inner = new FontManagerImpl();
            private readonly string _regularUri;
            private readonly string? _boldUri;
            private string? _familyName;

            public InstalledFamilyFontManagerImpl(string regularUri, string? boldUri = null)
            {
                _regularUri = regularUri;
                _boldUri = boldUri;
            }

            /// <summary>Number of typefaces created from a stream. The platform matches above never
            /// go through this overload, so every count is a synthesis that copied the font file.</summary>
            public int StreamTypefaceCreations { get; private set; }

            private string FamilyName
            {
                get
                {
                    if (_familyName is null)
                    {
                        using var typeface = LoadTypeface(_regularUri);

                        _familyName = typeface.FamilyName;
                    }

                    return _familyName;
                }
            }

            public string GetDefaultFontFamilyName() => FamilyName;

            public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false) => new[] { FamilyName };

            public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
                FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                if (!string.Equals(familyName, FamilyName, StringComparison.OrdinalIgnoreCase))
                {
                    platformTypeface = null;

                    return false;
                }

                var skTypeface = LoadTypeface(_boldUri is not null && weight >= FontWeight.SemiBold ? _boldUri : _regularUri);

                var fontSimulations = FontSimulations.None;

                if ((int)weight >= 600 && !skTypeface.IsBold)
                {
                    fontSimulations |= FontSimulations.Bold;
                }

                if (style == FontStyle.Italic && !skTypeface.IsItalic)
                {
                    fontSimulations |= FontSimulations.Oblique;
                }

                platformTypeface = new SkiaTypeface(skTypeface, fontSimulations);

                return true;
            }

            public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
                FontStretch fontStretch, string? familyName, CultureInfo? culture,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                var skTypeface = LoadTypeface(_regularUri);

                if (!skTypeface.ContainsGlyph(codepoint))
                {
                    skTypeface.Dispose();

                    platformTypeface = null;

                    return false;
                }

                platformTypeface = new SkiaTypeface(skTypeface, FontSimulations.None);

                return true;
            }

            public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                StreamTypefaceCreations++;

                return _inner.TryCreateGlyphTypeface(stream, fontSimulations, out platformTypeface);
            }

            public bool TryGetFamilyTypefaces(string familyName,
                [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
            {
                familyTypefaces = null;

                if (!string.Equals(familyName, FamilyName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var typefaces = new List<Typeface> { new(FamilyName) };

                if (_boldUri is not null)
                {
                    typefaces.Add(new Typeface(FamilyName, FontStyle.Normal, FontWeight.Bold));
                }

                familyTypefaces = typefaces;

                return true;
            }

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
}
