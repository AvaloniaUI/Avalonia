#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    /// <summary>
    /// Exercises the tiered <see cref="FontCollectionBase.TryMatchCharacter(int, FontStyle, FontWeight, FontStretch, string?, CultureInfo?, out Typeface)"/> algorithm
    /// (Tier A → Tier E) using a small set of embedded test fonts and a stub subclass
    /// that intercepts the platform-fallback hook.
    /// </summary>
    public class FontCollectionTryMatchCharacterTests
    {
        private const string AssetsNamespace = "Avalonia.Skia.UnitTests.Assets";

        private const int ArabicAlef = 0x0627;     // 'ا' — Arabic
        private const int HebrewAlef = 0x05D0;     // 'א' — Hebrew
        private const int CjkIchi = 0x4E00;        // '一' — not covered by any loaded test font

        [Fact]
        public void TierA_Returns_Requested_Family_When_It_Covers_The_Codepoint()
        {
            using var app = StartApp();

            var collection = BuildCollection(
                "Inter-Regular.ttf",
                "NotoSans-Italic.ttf",
                "NotoSansArabic-Regular.ttf");

            Assert.True(collection.TryMatchCharacter(
                ArabicAlef, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                familyName: "Noto Sans Arabic", culture: null, out var match));

            Assert.Equal("Noto Sans Arabic", FamilyOf(match));
        }

        [Fact]
        public void TierA_Falls_Through_When_Requested_Family_Does_Not_Cover_Codepoint()
        {
            // "Noto Mono" sorts alphabetically before "Noto Sans Arabic" so a non-coverage-checked
            // implementation that simply returned the requested family would yield Noto Mono.
            using var app = StartApp();

            var collection = BuildCollection(
                "NotoMono-Regular.ttf",
                "NotoSansArabic-Regular.ttf");

            Assert.True(collection.TryMatchCharacter(
                ArabicAlef, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                familyName: "Noto Mono", culture: null, out var match));

            Assert.Equal("Noto Sans Arabic", FamilyOf(match));
        }

        [Fact]
        public void TierB_Subsequent_Calls_Return_The_Same_Family()
        {
            using var app = StartApp();

            var collection = BuildCollection(
                "Inter-Regular.ttf",
                "NotoSansArabic-Regular.ttf",
                "NotoSansHebrew-Regular.ttf");

            Assert.True(collection.TryMatchCharacter(
                ArabicAlef, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                null, CultureInfo.GetCultureInfo("ar-SA"), out var first));

            for (var i = 0; i < 25; i++)
            {
                Assert.True(collection.TryMatchCharacter(
                    ArabicAlef, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                    null, CultureInfo.GetCultureInfo("ar-SA"), out var subsequent));

                Assert.Equal(FamilyOf(first), FamilyOf(subsequent));
            }
        }

        [Fact]
        public void TierC_Skips_Families_That_Do_Not_Cover_The_Codepoint()
        {
            using var app = StartApp();

            var collection = BuildCollection(
                "NotoMono-Regular.ttf",          // Latin only
                "Inter-Regular.ttf",             // Latin only
                "NotoSansArabic-Regular.ttf");   // covers ا

            Assert.True(collection.TryMatchCharacter(
                ArabicAlef, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                null, null, out var match));

            Assert.Equal("Noto Sans Arabic", FamilyOf(match));
        }

        [Fact]
        public void TierC_Picks_A_Covering_Family_Even_When_No_Culture_Is_Supplied()
        {
            using var app = StartApp();

            var collection = BuildCollection(
                "Inter-Regular.ttf",
                "NotoSans-Italic.ttf",
                "NotoSansHebrew-Regular.ttf");

            Assert.True(collection.TryMatchCharacter(
                HebrewAlef, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                null, null, out var match));

            Assert.Equal("Noto Sans Hebrew", FamilyOf(match));
        }

        [Fact]
        public void TierC_Prefers_Culture_Matched_Family_Over_Alphabetical_Order()
        {
            using var app = StartApp();

            // Both Hebrew and Arabic fonts are loaded but neither covers the requested codepoint;
            // the only family that does is "Noto Sans Arabic". Even if the algorithm did not
            // filter by coverage, the requested ar-SA culture should still resolve to it.
            var collection = BuildCollection(
                "NotoSansHebrew-Regular.ttf",
                "NotoSansArabic-Regular.ttf",
                "Inter-Regular.ttf");

            Assert.True(collection.TryMatchCharacter(
                ArabicAlef, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                null, CultureInfo.GetCultureInfo("ar-SA"), out var match));

            Assert.Equal("Noto Sans Arabic", FamilyOf(match));
        }

        [Fact]
        public void TierD_Platform_Fallback_Is_Invoked_When_Cache_Sweep_Has_No_Covering_Family()
        {
            using var app = StartApp();

            var collection = new RecordingFontCollection(new Uri("fonts:tierD-positive", UriKind.Absolute));
            LoadFonts(collection, "Inter-Regular.ttf");

            // The fallback resolves to a known typeface that is *also* registered in the cache
            // (so BuildTypefaceWithSynthesis succeeds against the returned glyph typeface).
            Assert.True(collection.TryGetGlyphTypeface(
                "Inter", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var interGt));

            collection.PlatformFallbackResult = interGt;

            Assert.True(collection.TryMatchCharacter(
                CjkIchi, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                null, CultureInfo.GetCultureInfo("ja-JP"), out var match));

            Assert.Equal("Inter", FamilyOf(match));
            Assert.Equal(1, collection.PlatformCallCount);
        }

        [Fact]
        public void TierD_Platform_Fallback_Is_Invoked_At_Most_Once_Per_Script_Culture_Pair()
        {
            using var app = StartApp();

            var collection = new RecordingFontCollection(new Uri("fonts:tierD-cache", UriKind.Absolute));
            LoadFonts(collection, "Inter-Regular.ttf");
            collection.PlatformFallbackResult = null; // negative platform answer

            for (var i = 0; i < 5; i++)
            {
                collection.TryMatchCharacter(
                    CjkIchi, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                    null, CultureInfo.GetCultureInfo("ja-JP"), out _);
            }

            Assert.Equal(1, collection.PlatformCallCount);
        }

        [Fact]
        public void TierD_Negative_Platform_Result_Is_Cached_Per_Script_Culture()
        {
            using var app = StartApp();

            var collection = new RecordingFontCollection(new Uri("fonts:tierD-negcache", UriKind.Absolute));
            LoadFonts(collection, "Inter-Regular.ttf");
            collection.PlatformFallbackResult = null;

            collection.TryMatchCharacter(
                CjkIchi, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                null, CultureInfo.GetCultureInfo("ja-JP"), out _);

            collection.TryMatchCharacter(
                CjkIchi, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                null, CultureInfo.GetCultureInfo("ko-KR"), out _);

            collection.TryMatchCharacter(
                CjkIchi, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                null, CultureInfo.GetCultureInfo("ja-JP"), out _);

            // ja-JP: 1 call + cached. ko-KR: 1 call. ja-JP again: cached. Total 2.
            Assert.Equal(2, collection.PlatformCallCount);
        }

        [Fact]
        public void Returns_False_When_No_Family_Covers_And_Platform_Has_No_Fallback()
        {
            using var app = StartApp();

            var collection = new RecordingFontCollection(new Uri("fonts:no-match", UriKind.Absolute));
            LoadFonts(collection, "Inter-Regular.ttf");
            collection.PlatformFallbackResult = null;

            Assert.False(collection.TryMatchCharacter(
                CjkIchi, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                null, null, out _));
        }

        // Regression for the "中华人民共和国 second codepoint is tofu" report. A positive (script,
        // culture) bucket entry records a preferred family, but it must not suppress the Tier D
        // platform lookup for another same-script codepoint that family cannot cover. Here 中 (U+4E2D)
        // resolves to the Noto Sans SC subset, which lacks the Simplified-only 华 (U+534E); 华 must then
        // reach the platform (which can place it) rather than be denied and rendered as tofu.
        [Fact]
        public void TierD_Positive_Script_Cache_Does_Not_Block_Platform_For_A_Codepoint_The_Bucket_Font_Lacks()
        {
            using var app = StartApp();

            const int zhong = 0x4E2D; // 中 — covered by the Noto Sans SC subset
            const int hua = 0x534E;   // 华 — Simplified-only; absent from the SC subset

            var collection = new RecordingFontCollection(new Uri("fonts:bucket-coverage", UriKind.Absolute));

            // Latin primary (covers neither ideograph) plus the SC subset that covers 中 but not 华.
            LoadFonts(collection, "NotoMono-Regular.ttf");
            LoadFontsFromFontsNamespace(collection, "NotoSansSC-Subset.ttf");

            // The SC subset is Thin (weight 100). Resolve at that weight so the Tier C exact-key upgrade
            // (covered by the dedicated weight test) does not fire here, leaving this test to isolate the
            // Tier D positive-bucket behaviour and its platform-call count.
            const FontWeight weight = FontWeight.Thin;

            Assert.True(collection.TryGetGlyphTypeface(
                "Noto Sans SC", FontStyle.Normal, weight, FontStretch.Normal, out var scGt));
            Assert.True(scGt.CharacterToGlyphMap.TryGetGlyph(zhong, out _));
            Assert.False(scGt.CharacterToGlyphMap.TryGetGlyph(hua, out _));

            // The platform can place 华 (MiSans covers it). It is deliberately NOT in the collection,
            // so only Tier D can supply it.
            var miSans = LoadStandaloneGlyphTypeface("Avalonia.Skia.UnitTests.Assets.MiSans-Normal.ttf");
            Assert.True(miSans.CharacterToGlyphMap.TryGetGlyph(hua, out _));
            collection.PlatformFallbackResult = miSans;

            var zh = CultureInfo.GetCultureInfo("zh-CN");

            // 中 resolves from the loaded cache (Tier C) and writes the (Han, zh-CN) bucket. The platform
            // is not consulted for it.
            Assert.True(collection.TryMatchCharacter(
                zhong, FontStyle.Normal, weight, FontStretch.Normal, null, zh, out var zhongMatch));
            Assert.Equal("Noto Sans SC", FamilyOf(zhongMatch));
            Assert.Equal(0, collection.PlatformCallCount);

            // 华 shares the bucket but the bucket font lacks it. Before the fix the positive entry made
            // Tier D treat the bucket as "platform already attempted", so 华 returned no match. It must
            // now reach the platform and resolve to a font that covers it.
            Assert.True(collection.TryMatchCharacter(
                hua, FontStyle.Normal, weight, FontStretch.Normal, null, zh, out var huaMatch));
            Assert.Equal("MiSans Normal", FamilyOf(huaMatch));
            Assert.Equal(1, collection.PlatformCallCount);
        }

        [Fact]
        public void Match_Typeface_Carries_The_Requested_Style_Weight_And_Stretch()
        {
            using var app = StartApp();

            var collection = BuildCollection(
                "Inter-Regular.ttf",
                "NotoSansArabic-Regular.ttf");

            Assert.True(collection.TryMatchCharacter(
                ArabicAlef, FontStyle.Italic, FontWeight.Bold, FontStretch.Condensed,
                null, null, out var match));

            Assert.Equal(FontStyle.Italic, match.Style);
            Assert.Equal(FontWeight.Bold, match.Weight);
            Assert.Equal(FontStretch.Condensed, match.Stretch);
        }

        // Regression for the "Normal CJK fallback renders bold" report, generalized across the whole
        // FontCollectionKey. The (script, culture) bucket is key-agnostic, so once one face of a fallback
        // family is resolved, a later request that differs in ANY axis (weight via Bold, style via
        // Oblique) must still resolve its own face, not reuse the cached neighbour. Stretch travels the
        // same path. The leak is order-dependent, so both orders are checked.
        [Theory]
        [InlineData(FontSimulations.Bold, FontSimulations.None)]    // weight: Bold then upright Normal (Sandbox order)
        [InlineData(FontSimulations.None, FontSimulations.Bold)]    // weight: reverse
        [InlineData(FontSimulations.Oblique, FontSimulations.None)] // style: Italic then upright
        [InlineData(FontSimulations.None, FontSimulations.Oblique)] // style: reverse
        public void Fallback_Resolves_The_Requested_Typeface_Not_A_Cached_Neighbour(
            FontSimulations first, FontSimulations second)
        {
            using var app = StartApp();

            const int aleph = 0x05D0; // Hebrew — absent from any Latin primary, so it needs fallback.

            // One family ("Noto Sans Hebrew") exposed by the platform as several faces (Regular plus a
            // synthetic Bold and Oblique), modelling a .ttc whose styles/weights are separate faces. All
            // cover aleph (same underlying font).
            var faces = new Dictionary<FontSimulations, GlyphTypeface>
            {
                [FontSimulations.None] = CreateGlyphTypeface(FontSimulations.None),
                [FontSimulations.Bold] = CreateGlyphTypeface(FontSimulations.Bold),
                [FontSimulations.Oblique] = CreateGlyphTypeface(FontSimulations.Oblique),
            };

            var collection = new KeyedFallbackCollection(new Uri("fonts:key", UriKind.Absolute), aleph);

            foreach (var face in faces.Values)
            {
                collection.AddPlatformFace(face);
            }

            var firstFace = faces[first];
            var secondFace = faces[second];
            var culture = CultureInfo.GetCultureInfo("he-IL");

            // First face: resolved via the platform (Tier D) and pins the bucket to the family.
            Assert.True(collection.TryMatchCharacter(
                aleph, firstFace.Style, firstFace.Weight, FontStretch.Normal, null, culture, out _));

            // Second face: same family, same bucket, different key. It must come back as its own key.
            Assert.True(collection.TryMatchCharacter(
                aleph, secondFace.Style, secondFace.Weight, FontStretch.Normal, null, culture, out _));

            Assert.True(collection.TryGetGlyphTypeface(
                secondFace.FamilyName, secondFace.Style, secondFace.Weight, FontStretch.Normal, out var secondResult));
            Assert.Equal(secondFace.ToFontCollectionKey(), secondResult.ToFontCollectionKey());

            // The first face stays correct too.
            Assert.True(collection.TryGetGlyphTypeface(
                firstFace.FamilyName, firstFace.Style, firstFace.Weight, FontStretch.Normal, out var firstResult));
            Assert.Equal(firstFace.ToFontCollectionKey(), firstResult.ToFontCollectionKey());
        }

        // Regression for "中华人民共和国: the first glyph is Normal but 华人民共和国 stay bold". A codepoint
        // the bucket family cannot cover (the Simplified-only 华 when the bucket is a JP font) skips Tier B
        // and lands in the Tier C sweep, where only a neighbouring-key face cached by an earlier run is
        // available. The exact-key upgrade must run in Tier C too. Modelled with Tamil, which is not
        // locale-sensitive, so resolving with no culture skips Tier B and drives the request into Tier C.
        [Theory]
        [InlineData(FontSimulations.Bold, FontSimulations.None)] // Sandbox order: Bold block above Normal one
        [InlineData(FontSimulations.None, FontSimulations.Bold)] // reverse
        public void TierC_Sweep_Resolves_The_Requested_Weight_Not_A_Cached_Neighbour(
            FontSimulations first, FontSimulations second)
        {
            using var app = StartApp();

            const int tamilKa = 0x0B95; // க — Tamil; not locale-sensitive, so culture=null skips Tier B.
            const string tamil = "Avalonia.Skia.UnitTests.Assets.NotoSansTamil-Regular.ttf";

            var faces = new Dictionary<FontSimulations, GlyphTypeface>
            {
                [FontSimulations.None] = CreateGlyphTypeface(tamil, FontSimulations.None),
                [FontSimulations.Bold] = CreateGlyphTypeface(tamil, FontSimulations.Bold),
            };

            Assert.Equal(FontWeight.Normal, faces[FontSimulations.None].Weight);
            Assert.Equal(FontWeight.Bold, faces[FontSimulations.Bold].Weight);

            var collection = new KeyedFallbackCollection(new Uri("fonts:tierc", UriKind.Absolute), tamilKa);

            foreach (var face in faces.Values)
            {
                collection.AddPlatformFace(face);
            }

            var firstFace = faces[first];
            var secondFace = faces[second];

            // culture=null on a non-locale-sensitive script skips Tier B, so the second request lands in
            // the Tier C sweep, where only the first (neighbouring-key) face is cached.
            Assert.True(collection.TryMatchCharacter(
                tamilKa, firstFace.Style, firstFace.Weight, FontStretch.Normal, null, null, out _));
            Assert.True(collection.TryMatchCharacter(
                tamilKa, secondFace.Style, secondFace.Weight, FontStretch.Normal, null, null, out _));

            Assert.True(collection.TryGetGlyphTypeface(
                secondFace.FamilyName, secondFace.Style, secondFace.Weight, FontStretch.Normal, out var secondResult));
            Assert.Equal(secondFace.ToFontCollectionKey(), secondResult.ToFontCollectionKey());
        }

        private static IDisposable StartApp() =>
            UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

        private static TestFontCollection BuildCollection(params string[] assetFileNames)
        {
            var collection = new TestFontCollection(new Uri("fonts:test", UriKind.Absolute));
            LoadFonts(collection, assetFileNames);
            return collection;
        }

        private static void LoadFonts(FontCollectionBase collection, params string[] assetFileNames)
        {
            var loader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            foreach (var fileName in assetFileNames)
            {
                var uri = new Uri($"resm:{AssetsNamespace}.{fileName}?assembly=Avalonia.Skia.UnitTests", UriKind.Absolute);
                using var stream = loader.Open(uri);
                Assert.True(collection.TryAddGlyphTypeface(stream, out _));
            }
        }

        // The subset test fonts live in the Fonts resource namespace rather than Assets.
        private static void LoadFontsFromFontsNamespace(FontCollectionBase collection, params string[] fileNames)
        {
            var loader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            foreach (var fileName in fileNames)
            {
                var uri = new Uri($"resm:Avalonia.Skia.UnitTests.Fonts.{fileName}?assembly=Avalonia.Skia.UnitTests", UriKind.Absolute);
                using var stream = loader.Open(uri);
                Assert.True(collection.TryAddGlyphTypeface(stream, out _));
            }
        }

        // Loads a font into a throwaway collection and returns its glyph typeface, so the platform
        // stub can return it without the font being present in the collection under test.
        private static GlyphTypeface LoadStandaloneGlyphTypeface(string resourceName)
        {
            var loader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
            var sink = new TestFontCollection(new Uri("fonts:sink", UriKind.Absolute));

            using var stream = loader.Open(new Uri($"resm:{resourceName}?assembly=Avalonia.Skia.UnitTests", UriKind.Absolute));
            Assert.True(sink.TryAddGlyphTypeface(stream, out var glyphTypeface));

            return glyphTypeface!;
        }

        // Builds a Noto Sans Hebrew glyph typeface at the requested simulations. None is the Regular
        // face; Bold and Oblique report weight Bold / style Italic respectively while keeping the same
        // family name and cmap, so the faces model one family at several keys.
        private static GlyphTypeface CreateGlyphTypeface(FontSimulations simulations)
            => CreateGlyphTypeface($"{AssetsNamespace}.NotoSansHebrew-Regular.ttf", simulations);

        private static GlyphTypeface CreateGlyphTypeface(string resourceName, FontSimulations simulations)
        {
            var loader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
            var fontManagerImpl = AvaloniaLocator.Current.GetRequiredService<IFontManagerImpl>();

            using var stream = loader.Open(new Uri($"resm:{resourceName}?assembly=Avalonia.Skia.UnitTests", UriKind.Absolute));

            Assert.True(fontManagerImpl.TryCreateGlyphTypeface(stream, simulations, out var platformTypeface));

            var glyphTypeface = GlyphTypeface.TryCreate(platformTypeface, simulations);
            Assert.NotNull(glyphTypeface);

            return glyphTypeface!;
        }

        private static string FamilyOf(Typeface typeface)
        {
            // Fallback typefaces are returned with FontFamily.Name == "<collectionKey>#<familyName>".
            var name = typeface.FontFamily.Name;
            var hashIndex = name.LastIndexOf('#');
            return hashIndex >= 0 ? name[(hashIndex + 1)..] : name;
        }

        private class TestFontCollection(Uri key) : FontCollectionBase
        {
            public override Uri Key { get; } = key;
        }

        private class RecordingFontCollection(Uri key) : FontCollectionBase
        {
            public override Uri Key { get; } = key;

            private int _platformCallCount;

            public GlyphTypeface? PlatformFallbackResult { get; set; }

            public int PlatformCallCount => _platformCallCount;

            protected override bool TryMatchCharacterFromPlatform(
                int codepoint,
                FontCollectionKey key,
                string? familyName,
                CultureInfo? culture,
                [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
            {
                Interlocked.Increment(ref _platformCallCount);

                glyphTypeface = PlatformFallbackResult;
                return glyphTypeface is not null;
            }
        }

        // A platform-backed collection holding one fallback family at several keys (style/weight). Its
        // single platform hook serves a face for an EXACT requested key only, modelling SystemFontCollection
        // over a .ttc, so the key-honouring fallback path (including the exact-key upgrade, which reuses
        // TryMatchCharacterFromPlatform) is exercised deterministically.
        private sealed class KeyedFallbackCollection : FontCollectionBase
        {
            private readonly Dictionary<FontCollectionKey, GlyphTypeface> _platformFaces = new();
            private readonly int _coveredCodepoint;

            public KeyedFallbackCollection(Uri key, int coveredCodepoint)
            {
                Key = key;
                _coveredCodepoint = coveredCodepoint;
            }

            public override Uri Key { get; }

            public void AddPlatformFace(GlyphTypeface face) => _platformFaces[face.ToFontCollectionKey()] = face;

            protected override bool TryMatchCharacterFromPlatform(
                int codepoint,
                FontCollectionKey key,
                string? familyName,
                CultureInfo? culture,
                [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
            {
                glyphTypeface = null;

                if (codepoint != _coveredCodepoint || !_platformFaces.TryGetValue(key, out var face))
                {
                    return false;
                }

                // Register the matched face, as SystemFontCollection does, so later tiers can find it.
                TryAddGlyphTypeface(face.FamilyName, key, face);
                glyphTypeface = face;
                return true;
            }
        }
    }
}
