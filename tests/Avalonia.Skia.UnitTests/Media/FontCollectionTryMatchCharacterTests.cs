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
    }
}
