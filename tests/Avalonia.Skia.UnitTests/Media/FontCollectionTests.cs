#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
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
        public void Should_Cache_A_Heavier_Weight_Simulated_On_The_Heaviest_Face()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontCollection = new TestSystemFontCollection(FontManager.Current.PlatformImpl);

                // Arial's heaviest face is Black, so ExtraBlack is Black with a bold simulation.
                Assert.True(fontCollection.TryGetGlyphTypeface("Arial", FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal(FontSimulations.Bold, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.ExtraBlack, glyphTypeface.Weight);

                Assert.True(fontCollection.GlyphTypefaceCache.TryGetValue("Arial", out var glyphTypefaces));

                Assert.Single(glyphTypefaces);

                Assert.True(glyphTypefaces.ContainsKey(new FontCollectionKey(FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal)));

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

        [Theory]
        [InlineData(FontStyle.Normal, 600, FontSimulations.Bold, 600)]
        [InlineData(FontStyle.Normal, 700, FontSimulations.Bold, 700)]
        [InlineData(FontStyle.Normal, 800, FontSimulations.Bold, 800)]
        [InlineData(FontStyle.Italic, 400, FontSimulations.Oblique, 400)]
        [InlineData(FontStyle.Italic, 700, FontSimulations.Bold | FontSimulations.Oblique, 700)]
        public void Should_Use_A_Face_The_Platform_Simulated_Itself(FontStyle style, int weight,
            FontSimulations expectedSimulations, int expectedWeight)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri,
                matching: PlatformMatching.SimulatedFace);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", style, (FontWeight)weight, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);

                // The platform already applied these simulations, so they are reported, not applied again.
                Assert.Equal(expectedSimulations, glyphTypeface.FontSimulations);
                Assert.Equal((FontWeight)expectedWeight, glyphTypeface.Weight);
                Assert.Equal(style, glyphTypeface.Style);
            }
        }

        [Fact]
        public void Should_Simulate_Bold_On_The_Real_Bold_Face_For_A_Heavier_Request()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(InterRegularUri, InterBoldUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.ExtraBold, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);

                Assert.Equal(FontSimulations.Bold, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.ExtraBold, glyphTypeface.Weight);
                Assert.True(((SkiaTypeface)glyphTypeface.PlatformTypeface).SKTypeface.IsBold);
            }
        }

        [Fact]
        public void Should_Accept_An_Oblique_Face_For_An_Italic_Request()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri,
                matching: PlatformMatching.ObliqueFace);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Italic, FontWeight.Normal, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);

                // A slanted face already satisfies a slanted request; slanting it again would double the slant.
                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(FontStyle.Oblique, glyphTypeface.PlatformTypeface.Style);
            }
        }

        [Fact]
        public void Should_Create_A_Synthetic_Face_From_The_Platform_Typeface_It_Already_Holds()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var regular));

                Assert.True(fontCollection.TryCreateSyntheticGlyphTypeface(
                    regular, FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var synthetic));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);

                Assert.Equal(FontSimulations.Bold, synthetic.FontSimulations);
                Assert.Equal(FontWeight.Black, synthetic.Weight);
                Assert.Same(((SkiaTypeface)regular.PlatformTypeface).SKTypeface,
                    ((SkiaTypeface)synthetic.PlatformTypeface).SKTypeface);
            }
        }

        [Fact]
        public void Should_Keep_The_Source_Simulations_In_A_Synthetic_Face()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Normal, FontWeight.Bold, FontStretch.Normal, out var bold));

                Assert.Equal(FontSimulations.Bold, bold.FontSimulations);

                Assert.True(fontCollection.TryCreateSyntheticGlyphTypeface(
                    bold, FontStyle.Italic, FontWeight.Bold, FontStretch.Normal, out var boldItalic));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);
                Assert.Equal(FontSimulations.Bold | FontSimulations.Oblique, boldItalic.FontSimulations);
                Assert.Equal(FontSimulations.Bold | FontSimulations.Oblique, boldItalic.PlatformTypeface.FontSimulations);
            }
        }

        [Fact]
        public void Should_Keep_Only_The_Requested_Source_Simulations_In_A_Synthetic_Face()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Italic, FontWeight.Bold, FontStretch.Normal, out var boldItalic));

                Assert.Equal(FontSimulations.Bold | FontSimulations.Oblique, boldItalic.FontSimulations);

                Assert.True(fontCollection.TryCreateSyntheticGlyphTypeface(
                    boldItalic, FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var black));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);
                Assert.Equal(FontSimulations.Bold, black.FontSimulations);
                Assert.Equal(FontSimulations.Bold, black.PlatformTypeface.FontSimulations);
                Assert.Equal(FontStyle.Normal, black.Style);
            }
        }

        [Fact]
        public void Should_Not_Create_An_Upright_Variant_Of_A_Face_The_Platform_Slanted()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri,
                matching: PlatformMatching.SimulatedFace);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Italic, FontWeight.Normal, FontStretch.Normal, out var italic));

                Assert.Equal(FontSimulations.Oblique, italic.PlatformTypeface.FontSimulations);

                // The slant is part of the platform's typeface, so no variant of it is upright.
                Assert.False(fontCollection.TryCreateSyntheticGlyphTypeface(
                    italic, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal, out _));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);
            }
        }

        [Fact]
        public void Should_Use_The_Platform_Face_For_An_Inexact_Match_After_Caching_A_Slanted_Face()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(InterRegularUri, InterBoldUri,
                matching: PlatformMatching.SimulatedFace);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Italic, FontWeight.Normal, FontStretch.Normal, out var italic));

                Assert.Equal(FontSimulations.Oblique, italic.FontSimulations);

                // The platform answers SemiBold with the real Bold face, which is not the requested key.
                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.SemiBold, FontStretch.Normal, out var semiBold));

                Assert.Equal(FontStyle.Normal, semiBold.Style);
                Assert.Equal(FontSimulations.None, semiBold.FontSimulations);
                Assert.True(((SkiaTypeface)semiBold.PlatformTypeface).SKTypeface.IsBold);
                Assert.Equal(0, fontManager.StreamTypefaceCreations);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.Bold, FontStretch.Normal, out var bold));

                Assert.Same(semiBold, bold);
            }
        }

        [Fact]
        public void Should_Prefer_The_Platform_Face_Over_A_Cached_Lighter_Face_For_An_Inexact_Match()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(InterRegularUri, InterBoldUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                // Caches the regular face under Medium.
                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.Medium, FontStretch.Normal, out var medium));

                Assert.False(((SkiaTypeface)medium.PlatformTypeface).SKTypeface.IsBold);

                // The platform answers SemiBold Italic with the real Bold face slanted.
                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Italic, FontWeight.SemiBold, FontStretch.Normal, out var semiBoldItalic));

                Assert.True(((SkiaTypeface)semiBoldItalic.PlatformTypeface).SKTypeface.IsBold);
                Assert.Equal(FontSimulations.Oblique, semiBoldItalic.FontSimulations);
                Assert.Equal(FontStyle.Italic, semiBoldItalic.Style);
                Assert.Equal(0, fontManager.StreamTypefaceCreations);
            }
        }

        [Fact]
        public void Should_Ask_The_Platform_Once_For_A_Repeated_Inexact_Match()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(InterRegularUri, InterBoldUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.SemiBold, FontStretch.Normal, out var first));

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.SemiBold, FontStretch.Normal, out var second));

                Assert.Same(first, second);
                Assert.Equal(1, fontManager.FamilyMatches);
            }
        }

        [Fact]
        public void Should_Reuse_The_Cached_Face_When_The_Platform_Answers_Another_Key_With_It()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(InterRegularUri, InterBoldUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.SemiBold, FontStretch.Normal, out var semiBold));

                // The platform answers 650 with the same Bold face it answered SemiBold with.
                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, (FontWeight)650, FontStretch.Normal, out var heavier));

                Assert.Equal(2, fontManager.FamilyMatches);
                Assert.Same(semiBold, heavier);
                Assert.NotSame(fontManager.LastFamilyMatch, ((SkiaTypeface)semiBold.PlatformTypeface).SKTypeface);

                Assert.True(fontCollection.GlyphTypefaceCache.TryGetValue("Inter", out var glyphTypefaces));
                Assert.Same(semiBold, glyphTypefaces[new FontCollectionKey(FontStyle.Normal, FontWeight.Bold,
                    FontStretch.Normal)]);
            }
        }

        [Fact]
        public void Should_Dispose_The_Platform_Typeface_It_Does_Not_Keep()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(InterRegularUri, InterBoldUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, FontWeight.SemiBold, FontStretch.Normal, out var semiBold));

                var kept = ((SkiaTypeface)semiBold.PlatformTypeface).SKTypeface;

                // The platform answers 650 with the Bold face the collection already holds, so the
                // platform typeface of that answer is not used.
                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Inter", FontStyle.Normal, (FontWeight)650, FontStretch.Normal, out var heavier));

                Assert.Same(semiBold, heavier);

                var unused = fontManager.LastFamilyMatch!;

                Assert.NotSame(kept, unused);
                Assert.Equal(IntPtr.Zero, unused.Handle);

                Assert.NotEqual(IntPtr.Zero, kept.Handle);
                Assert.True(semiBold.CharacterToGlyphMap.TryGetGlyph('A', out _));
            }
        }

        [Theory]
        [InlineData(FontStyle.Normal, 700)]
        [InlineData(FontStyle.Italic, 400)]
        [InlineData(FontStyle.Italic, 700)]
        public void Should_Ask_The_Platform_Once_For_A_Repeated_Colour_Font_Character_Fallback(FontStyle style,
            int weight)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri,
                fallbackUris: new[] { NotoSansJpUri, EmojiUri });

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                for (var i = 0; i < 2; i++)
                {
                    Assert.True(fontCollection.TryMatchCharacter(EmojiCodepoint, style, (FontWeight)weight,
                        FontStretch.Normal, null, null, out var match));

                    Assert.True(fontCollection.TryGetGlyphTypeface(match.FontFamily.Name, match.Style, match.Weight,
                        match.Stretch, out var glyphTypeface));

                    Assert.Equal(EmojiFamily, glyphTypeface.FamilyName);
                    Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                }

                // The regular face is asked for inside the first character match only.
                Assert.Equal(1, fontManager.CharacterMatches);
                Assert.Equal(1, fontManager.RegularColourFaceMatches);
            }
        }

        [Fact]
        public void Should_Synthesize_From_A_Stream_When_The_Platform_Typeface_Cannot_Simulate()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri, supportsSimulation: false);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var regular));

                Assert.True(fontCollection.TryCreateSyntheticGlyphTypeface(
                    regular, FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var synthetic));

                Assert.Equal(1, fontManager.StreamTypefaceCreations);
                Assert.Equal(FontSimulations.Bold, synthetic.FontSimulations);
            }
        }

        [Fact]
        public void Disposing_A_Synthetic_Face_Should_Keep_Its_Source_Usable()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    "Noto Mono", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var regular));

                Assert.True(fontCollection.TryCreateSyntheticGlyphTypeface(
                    regular, FontStyle.Normal, FontWeight.Black, FontStretch.Normal, out var synthetic));

                synthetic.Dispose();

                var source = ((SkiaTypeface)regular.PlatformTypeface).SKTypeface;

                Assert.NotEqual(IntPtr.Zero, source.Handle);
                Assert.True(source.ContainsGlyph('A'));
            }
        }

        [Theory]
        [InlineData(FontStyle.Normal, 700, false)]
        [InlineData(FontStyle.Italic, 400, false)]
        [InlineData(FontStyle.Italic, 700, false)]
        [InlineData(FontStyle.Normal, 700, true)]
        [InlineData(FontStyle.Italic, 400, true)]
        [InlineData(FontStyle.Italic, 700, true)]
        public void Should_Not_Simulate_A_Colour_Font_Face(FontStyle style, int weight, bool regularFirst)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(EmojiUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                if (regularFirst)
                {
                    Assert.True(fontCollection.TryGetGlyphTypeface(
                        EmojiFamily, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));
                }

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    EmojiFamily, style, (FontWeight)weight, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);

                // A colour glyph is fixed artwork: emboldening or slanting it only distorts it.
                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(FontSimulations.None, glyphTypeface.PlatformTypeface.FontSimulations);
                Assert.Equal(FontWeight.Normal, glyphTypeface.Weight);
                Assert.Equal(FontStyle.Normal, glyphTypeface.Style);

                Assert.True(fontCollection.GlyphTypefaceCache.TryGetValue(EmojiFamily, out var glyphTypefaces));
                Assert.True(glyphTypefaces.TryGetValue(
                    new FontCollectionKey(style, (FontWeight)weight, FontStretch.Normal), out var cached));
                Assert.Same(glyphTypeface, cached);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Not_Synthesize_A_Colour_Font_Face(bool supportsSimulation)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(EmojiUri, supportsSimulation: supportsSimulation);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    EmojiFamily, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var regular));

                Assert.False(fontCollection.TryCreateSyntheticGlyphTypeface(
                    regular, FontStyle.Italic, FontWeight.Bold, FontStretch.Normal, out _));

                Assert.Equal(0, fontManager.StreamTypefaceCreations);
            }
        }

        [Fact]
        public void Should_Answer_A_Repeated_Colour_Font_Face_Request_From_The_Cache()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(EmojiUri);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    EmojiFamily, FontStyle.Italic, FontWeight.Bold, FontStretch.Normal, out var first));

                var familyMatches = fontManager.FamilyMatches;

                Assert.True(fontCollection.TryGetGlyphTypeface(
                    EmojiFamily, FontStyle.Italic, FontWeight.Bold, FontStretch.Normal, out var second));

                Assert.Same(first, second);
                Assert.Equal(familyMatches, fontManager.FamilyMatches);
                Assert.Equal(0, fontManager.StreamTypefaceCreations);
            }
        }

        [Theory]
        [InlineData(FontStyle.Normal, 700)]
        [InlineData(FontStyle.Italic, 400)]
        [InlineData(FontStyle.Italic, 700)]
        public void Should_Not_Simulate_A_Colour_Font_Face_For_A_Character_Fallback(FontStyle style, int weight)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri,
                fallbackUris: new[] { NotoSansJpUri, EmojiUri });

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryMatchCharacter(EmojiCodepoint, style, (FontWeight)weight,
                    FontStretch.Normal, null, null, out var match));

                Assert.True(fontCollection.TryGetGlyphTypeface(match.FontFamily.Name, match.Style, match.Weight,
                    match.Stretch, out var glyphTypeface));

                Assert.Equal(EmojiFamily, glyphTypeface.FamilyName);
                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(FontSimulations.None, glyphTypeface.PlatformTypeface.FontSimulations);
                Assert.Equal(0, fontManager.StreamTypefaceCreations);
            }
        }

        [Fact]
        public void Should_Answer_A_Repeated_Colour_Font_Character_Fallback_From_The_Cache()
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri,
                fallbackUris: new[] { NotoSansJpUri, EmojiUri });

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryMatchCharacter(EmojiCodepoint, FontStyle.Italic, FontWeight.Bold,
                    FontStretch.Normal, null, null, out var first));

                Assert.True(fontCollection.TryGetGlyphTypeface(first.FontFamily.Name, first.Style, first.Weight,
                    first.Stretch, out var firstGlyphTypeface));

                var characterMatches = fontManager.CharacterMatches;
                var familyMatches = fontManager.FamilyMatches;

                Assert.True(fontCollection.TryMatchCharacter(EmojiCodepoint, FontStyle.Italic, FontWeight.Bold,
                    FontStretch.Normal, null, null, out var second));

                Assert.True(fontCollection.TryGetGlyphTypeface(second.FontFamily.Name, second.Style, second.Weight,
                    second.Stretch, out var secondGlyphTypeface));

                Assert.Same(firstGlyphTypeface, secondGlyphTypeface);
                Assert.Equal(characterMatches, fontManager.CharacterMatches);
                Assert.Equal(familyMatches, fontManager.FamilyMatches);
                Assert.Equal(0, fontManager.StreamTypefaceCreations);
            }
        }

        [Theory]
        [InlineData(FontStyle.Normal, 700, FontSimulations.Bold)]
        [InlineData(FontStyle.Italic, 400, FontSimulations.Oblique)]
        [InlineData(FontStyle.Italic, 700, FontSimulations.Bold | FontSimulations.Oblique)]
        public void Should_Simulate_A_Cjk_Character_Fallback(FontStyle style, int weight,
            FontSimulations expectedSimulations)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri,
                fallbackUris: new[] { NotoSansJpUri, EmojiUri });

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var fontCollection = new TestSystemFontCollection(fontManager);

                Assert.True(fontCollection.TryMatchCharacter(CjkCodepoint, style, (FontWeight)weight,
                    FontStretch.Normal, null, null, out var match));

                Assert.True(fontCollection.TryGetGlyphTypeface(match.FontFamily.Name, match.Style, match.Weight,
                    match.Stretch, out var glyphTypeface));

                Assert.Equal(CjkFamily, glyphTypeface.FamilyName);
                Assert.Equal(expectedSimulations, glyphTypeface.FontSimulations);
                Assert.Equal(0, fontManager.StreamTypefaceCreations);
            }
        }

        [Theory]
        [InlineData(FontStyle.Normal, 700, FontSimulations.Bold)]
        [InlineData(FontStyle.Italic, 400, FontSimulations.Oblique)]
        public void TextLayout_Should_Simulate_A_Cjk_Fallback_Run_But_Not_A_Colour_Emoji_Run(FontStyle style,
            int weight, FontSimulations expectedSimulations)
        {
            var fontManager = new InstalledFamilyFontManagerImpl(NotoMonoRegularUri,
                fallbackUris: new[] { NotoSansJpUri, EmojiUri });

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManager)))
            {
                var text = "Bold " + char.ConvertFromUtf32(CjkCodepoint) + " " + char.ConvertFromUtf32(EmojiCodepoint);

                var layout = new TextLayout(text, new Typeface("Noto Mono", style, (FontWeight)weight), 12,
                    Brushes.Black);

                var simulationsByFamily = new Dictionary<string, FontSimulations>();

                foreach (var textRun in layout.TextLines.Single().TextRuns.OfType<ShapedTextRun>())
                {
                    var glyphTypeface = textRun.ShapedBuffer.GlyphTypeface;

                    simulationsByFamily[glyphTypeface.FamilyName] = glyphTypeface.FontSimulations;
                }

                Assert.Equal(expectedSimulations, simulationsByFamily["Noto Mono"]);
                Assert.Equal(expectedSimulations, simulationsByFamily[CjkFamily]);
                Assert.Equal(FontSimulations.None, simulationsByFamily[EmojiFamily]);
            }
        }

        // U+1F600, covered only by the colour font.
        private const int EmojiCodepoint = 0x1F600;

        // U+4E2D, covered only by the CJK font.
        private const int CjkCodepoint = 0x4E2D;

        private const string EmojiFamily = "Twitter Color Emoji";

        private const string CjkFamily = "Noto Sans JP";

        // Its glyphs are SVG documents in the SVG table, with monochrome outlines for renderers
        // without SVG support.
        private const string EmojiUri =
            "resm:Avalonia.Skia.UnitTests.Assets.TwitterColorEmoji-SVGinOT.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string NotoSansJpUri =
            "resm:Avalonia.Skia.UnitTests.Fonts.NotoSansJP-Subset.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string NotoMonoRegularUri =
            "resm:Avalonia.Skia.UnitTests.Assets.NotoMono-Regular.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string InterRegularUri =
            "resm:Avalonia.Skia.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Skia.UnitTests";

        private const string InterBoldUri =
            "resm:Avalonia.Skia.UnitTests.Assets.Inter-Bold.ttf?assembly=Avalonia.Skia.UnitTests";

        /// <summary>How the platform answers a request for a face the family does not have.</summary>
        private enum PlatformMatching
        {
            /// <summary>The nearest face as designed, as CoreText and fontconfig return it.</summary>
            NearestFace,

            /// <summary>The nearest face with the missing bold or slant simulated inside the matched
            /// typeface, which then reports the simulated weight and an oblique slant, as DirectWrite
            /// returns it.</summary>
            SimulatedFace,

            /// <summary>A designed oblique face for any slanted request, as for a family whose only
            /// slanted face is oblique.</summary>
            ObliqueFace
        }

        /// <summary>
        /// Font manager with one installed family, and optionally more single-face families for
        /// character fallback, backed by embedded fonts. Only the platform match itself is replaced:
        /// the matched <see cref="SKTypeface"/> is wrapped the way <see cref="FontManagerImpl"/> wraps
        /// the typeface its platform matched.
        /// </summary>
        private sealed class InstalledFamilyFontManagerImpl : IFontManagerImpl
        {
            private readonly IFontManagerImpl _inner = new FontManagerImpl();
            private readonly string _regularUri;
            private readonly string? _boldUri;
            private readonly string[] _fallbackUris;
            private readonly PlatformMatching _matching;
            private readonly bool _supportsSimulation;
            private string? _familyName;
            private string[]? _fallbackFamilyNames;

            /// <param name="regularUri">The regular face of the family.</param>
            /// <param name="boldUri">The bold face of the family, if it has one.</param>
            /// <param name="matching">How a request for a missing face is answered.</param>
            /// <param name="supportsSimulation">When false, every platform typeface is handed out
            /// behind a wrapper that cannot create simulated variants, the shape of a backend without
            /// that support.</param>
            /// <param name="fallbackUris">Further installed families of a single face each. Character
            /// fallback tries the requested family first, then the family, then these in order.</param>
            public InstalledFamilyFontManagerImpl(string regularUri, string? boldUri = null,
                PlatformMatching matching = PlatformMatching.NearestFace, bool supportsSimulation = true,
                string[]? fallbackUris = null)
            {
                _regularUri = regularUri;
                _boldUri = boldUri;
                _matching = matching;
                _supportsSimulation = supportsSimulation;
                _fallbackUris = fallbackUris ?? Array.Empty<string>();
            }

            /// <summary>Number of typefaces created from a stream. The platform matches above never
            /// go through this overload, so every count is a synthesis that copied the font file.</summary>
            public int StreamTypefaceCreations { get; private set; }

            /// <summary>Number of family matches asked of the platform.</summary>
            public int FamilyMatches { get; private set; }

            /// <summary>Number of character matches asked of the platform.</summary>
            public int CharacterMatches { get; private set; }

            /// <summary>Number of family matches a character match asked of the platform to replace a
            /// colour font face with its regular upright face.</summary>
            public int RegularColourFaceMatches { get; private set; }

            /// <summary>The typeface the latest family match returned.</summary>
            public SKTypeface? LastFamilyMatch { get; private set; }

            private string FamilyName => _familyName ??= GetFamilyName(_regularUri);

            private string[] FallbackFamilyNames => _fallbackFamilyNames ??= Array.ConvertAll(_fallbackUris, GetFamilyName);

            public string GetDefaultFontFamilyName() => FamilyName;

            public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
            {
                var familyNames = new List<string> { FamilyName };

                familyNames.AddRange(FallbackFamilyNames);

                return familyNames.ToArray();
            }

            public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
                FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                FamilyMatches++;

                string uri;

                if (string.Equals(familyName, FamilyName, StringComparison.OrdinalIgnoreCase))
                {
                    uri = _boldUri is not null && weight >= FontWeight.SemiBold ? _boldUri : _regularUri;
                }
                else
                {
                    var index = IndexOfFallbackFamily(familyName);

                    if (index < 0)
                    {
                        platformTypeface = null;

                        return false;
                    }

                    uri = _fallbackUris[index];
                }

                var skTypeface = LoadTypeface(uri);

                // The weight and style the matched typeface reports, which differ from the design
                // values of the font file when the platform simulated the face.
                var faceWeight = (FontWeight)skTypeface.FontWeight;
                var faceStyle = skTypeface.FontStyle.Slant.ToAvalonia();
                var platformSimulations = FontSimulations.None;

                switch (_matching)
                {
                    case PlatformMatching.SimulatedFace:
                        if (weight >= FontWeight.SemiBold && faceWeight < FontWeight.SemiBold)
                        {
                            faceWeight = FontWeight.Bold;
                            platformSimulations |= FontSimulations.Bold;
                        }

                        if (style != FontStyle.Normal && faceStyle == FontStyle.Normal)
                        {
                            faceStyle = FontStyle.Oblique;
                            platformSimulations |= FontSimulations.Oblique;
                        }

                        break;
                    case PlatformMatching.ObliqueFace when style != FontStyle.Normal:
                        faceStyle = FontStyle.Oblique;
                        break;
                }

                LastFamilyMatch = skTypeface;

                platformTypeface = Wrap(FontManagerImpl.CreatePlatformTypeface(skTypeface, faceStyle, faceWeight,
                    (FontStretch)skTypeface.FontWidth, platformSimulations, style, weight, stretch,
                    SkiaTypeface.IsColorFont(skTypeface)));

                return true;
            }

            private IPlatformTypeface Wrap(IPlatformTypeface platformTypeface) => _supportsSimulation ?
                platformTypeface :
                new ReportedPlatformTypeface(platformTypeface, platformTypeface.Style, platformTypeface.Weight);

            public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
                FontStretch fontStretch, string? familyName, CultureInfo? culture,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                CharacterMatches++;

                var uris = new List<string> { _regularUri };

                uris.AddRange(_fallbackUris);

                // Like a platform font manager, prefer the requested family when it covers the codepoint.
                var requested = familyName is null ? -1 : IndexOfFallbackFamily(familyName);

                if (requested >= 0)
                {
                    uris.Insert(0, _fallbackUris[requested]);
                }

                foreach (var uri in uris)
                {
                    var skTypeface = LoadTypeface(uri);

                    if (skTypeface.ContainsGlyph(codepoint))
                    {
                        var matchedUri = uri;

                        // Each installed face here is the only face of its family, so the family
                        // match asked for a regular colour face is that same face.
                        platformTypeface = FontManagerImpl.CreatePlatformTypeface(skTypeface, fontStyle, fontWeight,
                            fontStretch, (_, _) =>
                            {
                                RegularColourFaceMatches++;

                                return LoadTypeface(matchedUri);
                            });

                        return true;
                    }

                    skTypeface.Dispose();
                }

                platformTypeface = null;

                return false;
            }

            public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                StreamTypefaceCreations++;

                if (!_inner.TryCreateGlyphTypeface(stream, fontSimulations, out platformTypeface))
                {
                    return false;
                }

                platformTypeface = Wrap(platformTypeface);

                return true;
            }

            public bool TryGetFamilyTypefaces(string familyName,
                [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
            {
                familyTypefaces = null;

                if (IndexOfFallbackFamily(familyName) >= 0)
                {
                    familyTypefaces = new List<Typeface> { new(familyName) };

                    return true;
                }

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

            private int IndexOfFallbackFamily(string familyName) => Array.FindIndex(FallbackFamilyNames,
                x => string.Equals(x, familyName, StringComparison.OrdinalIgnoreCase));

            private static string GetFamilyName(string uri)
            {
                using var typeface = LoadTypeface(uri);

                return typeface.FamilyName;
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

        /// <summary>
        /// Platform typeface that reports the given style and weight for an inner typeface and
        /// exposes nothing beyond <see cref="IPlatformTypeface"/>.
        /// </summary>
        private sealed class ReportedPlatformTypeface : IPlatformTypeface
        {
            private readonly IPlatformTypeface _inner;

            public ReportedPlatformTypeface(IPlatformTypeface inner, FontStyle style, FontWeight weight)
            {
                _inner = inner;
                Style = style;
                Weight = weight;
            }

            public string FamilyName => _inner.FamilyName;

            public FontWeight Weight { get; }

            public FontStyle Style { get; }

            public FontStretch Stretch => _inner.Stretch;

            public FontSimulations FontSimulations => _inner.FontSimulations;

            public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table) => _inner.TryGetTable(tag, out table);

            public bool TryGetStream([NotNullWhen(true)] out Stream? stream) => _inner.TryGetStream(out stream);

            public void Dispose() => _inner.Dispose();
        }
    }
}
