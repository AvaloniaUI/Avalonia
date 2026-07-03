#nullable enable

using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextCharactersTests
    {
        // Curated system fonts (see Start): "Noto Mono" is the primary; "DejaVu Sans" is a broad
        // fallback that covers Hebrew and a wide range of combining marks. The broad-coverage faces
        // bundled for other tests (AdobeBlank2VF, MiSans/NISC CJK) are excluded so that a CJK
        // codepoint genuinely has no fallback.
        private const string PrimaryFont = "Avalonia.Skia.UnitTests.Assets.NotoMono-Regular.ttf";
        private const string FallbackFont = "Avalonia.Skia.UnitTests.Fonts.DejaVuSans.ttf";

        // Tiny zh/ja regional subsets (a few glyphs each) of the Google Fonts Noto Sans SC / JP, with
        // distinct OS/2 codepage bits and localized family names so the culture-aware fallback scorer
        // can tell them apart. Both cover U+4E2D (中); only the JP subset covers U+3042 (あ).
        private const string NotoSansScFont = "Avalonia.Skia.UnitTests.Fonts.NotoSansSC-Subset.ttf";
        private const string NotoSansJpFont = "Avalonia.Skia.UnitTests.Fonts.NotoSansJP-Subset.ttf";

        // U+4E2D 中 — a CJK ideograph covered by neither curated font, and with no platform fallback,
        // so it has no match at all.
        private const int NoMatchCodepoint = 0x4E2D;

        // U+05D0 Hebrew aleph — covered by DejaVu Sans but not Noto Mono, so it resolves to a fallback.
        private const int FallbackCodepoint = 0x05D0;

        // F2 — a cluster that has no home (NoMatchCodepoint) immediately followed by one that does
        // (FallbackCodepoint). This used to make the .notdef recovery loop swallow the renderable
        // cluster into the tofu run.
        [Fact]
        public void GetShapeableCharacters_Does_Not_Swallow_Fallbackable_Cluster_After_Unmatchable_One()
        {
            using (Start(PrimaryFont, FallbackFont))
            {
                var fontManager = FontManager.Current;

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var defaultGlyphTypeface = defaultProperties.CachedGlyphTypeface;
                var defaultFontFamily = defaultProperties.Typeface.FontFamily;

                // Preconditions: the primary covers neither codepoint, the first has no fallback, the
                // second does.
                Assert.False(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(NoMatchCodepoint, out _));
                Assert.False(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(FallbackCodepoint, out _));

                Assert.False(fontManager.TryMatchCharacter(NoMatchCodepoint, FontStyle.Normal, FontWeight.Normal,
                    FontStretch.Normal, defaultFontFamily, null, out _));
                Assert.True(fontManager.TryMatchCharacter(FallbackCodepoint, FontStyle.Normal, FontWeight.Normal,
                    FontStretch.Normal, defaultFontFamily, null, out _));

                var text = string.Concat(
                    char.ConvertFromUtf32(NoMatchCodepoint),
                    char.ConvertFromUtf32(FallbackCodepoint)).AsMemory();

                var textCharacters = new TextCharacters(text, defaultProperties);

                var results = FormattingObjectPool.Instance.TextRunLists.Rent();

                try
                {
                    TextRunProperties? previousProperties = null;

                    textCharacters.GetShapeableCharacters(text, 0, fontManager, ref previousProperties, results);

                    // Before the fix this was a SINGLE coalesced .notdef run spanning both codepoints
                    // with the primary typeface — the Hebrew cluster was rendered as tofu even though a
                    // fallback exists. The recovery loop now stops at the fallbackable cluster.
                    Assert.Equal(2, results.Count);

                    // First run: the genuinely unmatchable cluster, left with the primary (tofu) typeface.
                    Assert.Equal(1, results[0].Length);
                    Assert.Equal(defaultProperties.Typeface, results[0].Properties!.Typeface);

                    // Second run: the Hebrew cluster, handed to a fallback that actually covers it.
                    Assert.Equal(1, results[1].Length);
                    Assert.NotEqual(defaultProperties.Typeface, results[1].Properties!.Typeface);
                    Assert.True(results[1].Properties!.CachedGlyphTypeface.CharacterToGlyphMap
                        .TryGetGlyph(FallbackCodepoint, out _));
                }
                finally
                {
                    FormattingObjectPool.RentedList<TextRun>? toReturn = results;
                    FormattingObjectPool.Instance.TextRunLists.Return(ref toReturn);
                }
            }
        }

        // F1 — a base+combining-mark cluster where the primary font has the base but not the mark, and
        // a fallback covers the whole cluster. The whole cluster must be handed to that fallback rather
        // than left on the primary (which would drop the mark).
        [Fact]
        public void GetShapeableCharacters_Prefers_A_Fallback_That_Covers_The_Whole_Cluster_Including_Marks()
        {
            using (Start(PrimaryFont, FallbackFont))
            {
                var fontManager = FontManager.Current;

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var defaultGlyphTypeface = defaultProperties.CachedGlyphTypeface;
                var defaultFontFamily = defaultProperties.Typeface.FontFamily;

                const int baseCodepoint = 'a';
                Assert.True(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(baseCodepoint, out _));

                // Probe for a combining mark the primary lacks but a fallback covers together with the
                // base. Probing keeps the test robust to the exact coverage of the embedded fonts.
                var mark = 0;

                foreach (var candidate in CombiningMarkCandidates)
                {
                    if (defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(candidate, out _))
                    {
                        continue; // primary already covers it - not a useful probe
                    }

                    if (fontManager.TryMatchCharacter(candidate, FontStyle.Normal, FontWeight.Normal,
                            FontStretch.Normal, defaultFontFamily, null, out var markTypeface)
                        && fontManager.TryGetGlyphTypeface(markTypeface, out var markGlyphTypeface)
                        && markGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(baseCodepoint, out _))
                    {
                        mark = candidate;
                        break;
                    }
                }

                Assert.True(mark != 0,
                    "No combining mark found that the primary font lacks but a fallback covers together with the base.");

                var text = ("a" + char.ConvertFromUtf32(mark)).AsMemory();

                var textCharacters = new TextCharacters(text, defaultProperties);

                var results = FormattingObjectPool.Instance.TextRunLists.Rent();

                try
                {
                    TextRunProperties? previousProperties = null;

                    textCharacters.GetShapeableCharacters(text, 0, fontManager, ref previousProperties, results);

                    // The base+mark cluster stays whole, on a font that covers the mark. Before the fix
                    // it was left on the primary (which has the base but not the mark), dropping the mark.
                    Assert.NotEmpty(results);

                    var firstRun = results[0];

                    Assert.Equal(text.Length, firstRun.Length);
                    Assert.NotEqual(defaultProperties.Typeface, firstRun.Properties!.Typeface);
                    Assert.True(firstRun.Properties!.CachedGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(mark, out _),
                        "The cluster's run uses a font that does not cover the combining mark.");
                }
                finally
                {
                    FormattingObjectPool.RentedList<TextRun>? toReturn = results;
                    FormattingObjectPool.Instance.TextRunLists.Return(ref toReturn);
                }
            }
        }

        // F5 — NUL characters are replaced with non-breaking WORD JOINER (U+2060), not ZERO WIDTH
        // SPACE (U+200B), which would introduce a line-break opportunity NUL never had.
        [Fact]
        public void GetShapeableCharacters_Replaces_Null_Characters_With_Non_Breaking_Word_Joiners()
        {
            using (Start(PrimaryFont, FallbackFont))
            {
                var fontManager = FontManager.Current;
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var text = "\0\0".AsMemory();

                var textCharacters = new TextCharacters(text, defaultProperties);

                var results = FormattingObjectPool.Instance.TextRunLists.Rent();

                try
                {
                    TextRunProperties? previousProperties = null;

                    textCharacters.GetShapeableCharacters(text, 0, fontManager, ref previousProperties, results);

                    Assert.Single(results);
                    Assert.Equal(text.Length, results[0].Length);

                    foreach (var c in results[0].Text.Span)
                    {
                        Assert.Equal((char)0x2060, c);
                    }
                }
                finally
                {
                    FormattingObjectPool.RentedList<TextRun>? toReturn = results;
                    FormattingObjectPool.Instance.TextRunLists.Return(ref toReturn);
                }
            }
        }

        // F4 — the previous run's font is reused as an anti-thrashing bias, but for a locale-sensitive
        // script (CJK Han unification) it must not be pinned across a culture change. A zh run's
        // Simplified-Chinese font must not carry into a following ja run; the ja run resolves to the
        // culture-appropriate Japanese font instead.
        [Fact]
        public void GetShapeableCharacters_Does_Not_Pin_Previous_Region_Font_Across_A_Culture_Change()
        {
            using (Start(PrimaryFont, NotoSansScFont, NotoSansJpFont))
            {
                var fontManager = FontManager.Current;
                var ja = CultureInfo.GetCultureInfo("ja-JP");
                var zh = CultureInfo.GetCultureInfo("zh-CN");

                // Previous run: the Simplified-Chinese font, resolved for a zh culture.
                var scTypeface = new Typeface(new FontFamily("fonts:SystemFonts#Noto Sans SC"));
                Assert.True(fontManager.TryGetGlyphTypeface(scTypeface, out var scGlyphTypeface));

                // Current run: a Latin primary that lacks the ideograph, under a ja culture.
                var defaultProperties = new GenericTextRunProperties(Typeface.Default, cultureInfo: ja);

                const int han = 0x4E2D; // 中 (a Han codepoint both regional fonts cover)

                // Preconditions: primary lacks 中; the zh font covers it; and the culture-aware fallback
                // for ja prefers the JP font over the SC font (distinct OS/2 codepage + localized names).
                Assert.False(defaultProperties.CachedGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(han, out _));
                Assert.True(scGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(han, out _));
                Assert.True(fontManager.TryMatchCharacter(han, FontStyle.Normal, FontWeight.Normal,
                    FontStretch.Normal, defaultProperties.Typeface.FontFamily, ja, out var jaMatch));
                Assert.True(fontManager.TryGetGlyphTypeface(jaMatch, out var jaMatchGlyphTypeface));
                Assert.Equal("Noto Sans JP", jaMatchGlyphTypeface.FamilyName);

                var text = char.ConvertFromUtf32(han).AsMemory();
                var textCharacters = new TextCharacters(text, defaultProperties);

                TextRunProperties? previousProperties = new GenericTextRunProperties(scTypeface, cultureInfo: zh);

                var results = FormattingObjectPool.Instance.TextRunLists.Rent();

                try
                {
                    textCharacters.GetShapeableCharacters(text, 0, fontManager, ref previousProperties, results);

                    Assert.Single(results);
                    Assert.True(fontManager.TryGetGlyphTypeface(results[0].Properties!.Typeface, out var runGlyphTypeface));

                    // With the fix, the zh→ja culture change on a locale-sensitive script skips reuse of
                    // the previous (SC) font, so the run resolves to the ja-appropriate JP font. Before
                    // the fix the SC font was pinned and this was "Noto Sans SC".
                    Assert.Equal("Noto Sans JP", runGlyphTypeface.FamilyName);
                }
                finally
                {
                    FormattingObjectPool.RentedList<TextRun>? toReturn = results;
                    FormattingObjectPool.Instance.TextRunLists.Return(ref toReturn);
                }
            }
        }

        // A spread of combining marks (all grapheme-cluster Extend) likely present in a broad fallback
        // font but absent from a minimal monospace primary. The F1 test picks the first workable one.
        private static readonly int[] CombiningMarkCandidates =
        {
            0x0316, 0x0317, 0x031C, 0x0323, 0x032E, 0x0333, 0x0359, 0x035C, 0x0360, 0x0361, 0x0362,
            0x0363, 0x036F, 0x0488, 0x0489, 0x1DC0, 0x1DC1, 0x20DD, 0x20E0,
        };

        private static IDisposable Start(params string[] fontResourceNames)
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface()));

            var fontManagerImpl = new CustomFontManagerImpl();

            AvaloniaLocator.CurrentMutable
                .Bind<IFontManagerImpl>().ToConstant(fontManagerImpl);

            var fontManager = new FontManager(fontManagerImpl);

            AvaloniaLocator.CurrentMutable
                .Bind<FontManager>().ToConstant(fontManager);

            // Register a curated system collection holding only the fonts each test needs. This excludes
            // the broad-coverage fonts bundled for other tests, so coverage is exactly the requested set.
            fontManager.AddFontCollection(new CuratedSystemFontCollection(fontResourceNames));

            return disposable;
        }

        private sealed class CuratedSystemFontCollection : FontCollectionBase
        {
            public CuratedSystemFontCollection(string[] fontResourceNames)
            {
                foreach (var name in fontResourceNames)
                {
                    TryAddFontSource(new Uri($"resm:{name}?assembly=Avalonia.Skia.UnitTests"));
                }
            }

            public override Uri Key => FontManager.SystemFontsKey;
        }
    }
}
