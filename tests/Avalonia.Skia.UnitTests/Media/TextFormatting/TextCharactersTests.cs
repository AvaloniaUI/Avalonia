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

        // A second broad fallback that covers Latin plus a range of combining marks but has no glyph
        // for U+FE0F - unlike DejaVu Sans, which maps the variation selector and would therefore hide
        // the default-ignorable bug.
        private const string NoVariationSelectorFallbackFont = "Avalonia.Skia.UnitTests.Assets.NotoSans-Italic.ttf";

        // Tiny zh/ja regional subsets (a few glyphs each) of the Google Fonts Noto Sans SC / JP, with
        // distinct OS/2 codepage bits and localized family names so the culture-aware fallback scorer
        // can tell them apart. Both cover U+4E2D (中); only the JP subset covers U+3042 (あ).
        private const string NotoSansScFont = "Avalonia.Skia.UnitTests.Fonts.NotoSansSC-Subset.ttf";
        private const string NotoSansJpFont = "Avalonia.Skia.UnitTests.Fonts.NotoSansJP-Subset.ttf";

        // A colour emoji font, of the kind every platform ships: it covers the emoji block and, like
        // practically every font, U+0020 - at an advance of its own that is not the primary's.
        private const string EmojiFont = "Avalonia.Skia.UnitTests.Assets.TwitterColorEmoji-SVGinOT.ttf";

        // U+1F642 🙂 — covered by the emoji font only.
        private const int EmojiCodepoint = 0x1F642;

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

                var mark = FindMarkCoveredByFallbackOnly(fontManager, defaultGlyphTypeface, defaultFontFamily,
                    baseCodepoint);

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

        // A default ignorable codepoint inside a cluster must not be treated as content the font has to
        // cover. U+FE0F is a nonspacing mark by general category, so it used to be demanded from every
        // candidate font; no text font maps it, which made the whole base+VS16+mark cluster unmatchable
        // and dropped it back onto the primary font (rendering the mark as .notdef).
        [Fact]
        public void GetShapeableCharacters_Ignores_Default_Ignorable_Codepoints_When_Matching_Cluster_Coverage()
        {
            using (Start(PrimaryFont, NoVariationSelectorFallbackFont))
            {
                var fontManager = FontManager.Current;

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var defaultGlyphTypeface = defaultProperties.CachedGlyphTypeface;
                var defaultFontFamily = defaultProperties.Typeface.FontFamily;

                const int baseCodepoint = 'a';
                const char variationSelector16 = '\uFE0F';

                Assert.True(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(baseCodepoint, out _));

                // The premise: no font here has a glyph for the variation selector, and none is expected
                // to - it is default ignorable.
                Assert.False(fontManager.TryMatchCharacter(variationSelector16, FontStyle.Normal,
                    FontWeight.Normal, FontStretch.Normal, defaultFontFamily, null, out _));

                var mark = FindMarkCoveredByFallbackOnly(fontManager, defaultGlyphTypeface, defaultFontFamily,
                    baseCodepoint);

                var text = ("a" + variationSelector16 + char.ConvertFromUtf32(mark)).AsMemory();

                var textCharacters = new TextCharacters(text, defaultProperties);

                var results = FormattingObjectPool.Instance.TextRunLists.Rent();

                try
                {
                    TextRunProperties? previousProperties = null;

                    textCharacters.GetShapeableCharacters(text, 0, fontManager, ref previousProperties, results);

                    Assert.NotEmpty(results);

                    var firstRun = results[0];

                    Assert.Equal(text.Length, firstRun.Length);
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

        // Probes for a combining mark the primary font lacks but a fallback covers together with the
        // base. Probing keeps the tests robust to the exact coverage of the embedded fonts.
        private static int FindMarkCoveredByFallbackOnly(FontManager fontManager, GlyphTypeface primary,
            FontFamily primaryFontFamily, int baseCodepoint)
        {
            foreach (var candidate in CombiningMarkCandidates)
            {
                if (primary.CharacterToGlyphMap.TryGetGlyph(candidate, out _))
                {
                    continue; // primary already covers it - not a useful probe
                }

                if (fontManager.TryMatchCharacter(candidate, FontStyle.Normal, FontWeight.Normal,
                        FontStretch.Normal, primaryFontFamily, null, out var markTypeface)
                    && fontManager.TryGetGlyphTypeface(markTypeface, out var markGlyphTypeface)
                    && markGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(baseCodepoint, out _))
                {
                    return candidate;
                }
            }

            Assert.Fail(
                "No combining mark found that the primary font lacks but a fallback covers together with the base.");

            return 0;
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

        // A fallback run must end where the primary font regains coverage, whitespace included.
        // Practically every font maps U+0020, so a run that is extended for as long as the fallback
        // has glyphs swallows the space that follows the fallback text and shapes it with the
        // fallback's space glyph - which is a full em in most emoji fonts.
        [Fact]
        public void GetShapeableCharacters_Does_Not_Absorb_Whitespace_Into_A_Fallback_Run()
        {
            using (Start(PrimaryFont, FallbackFont))
            {
                var fontManager = FontManager.Current;

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var defaultGlyphTypeface = defaultProperties.CachedGlyphTypeface;
                var defaultFontFamily = defaultProperties.Typeface.FontFamily;

                // Preconditions: the primary lacks the Hebrew letter but covers both the space and the
                // letter after it, and the fallback that covers the Hebrew letter maps the space too -
                // which is what lets the fallback run reach past the letter today.
                Assert.False(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(FallbackCodepoint, out _));
                Assert.True(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(' ', out _));
                Assert.True(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph('b', out _));

                Assert.True(fontManager.TryMatchCharacter(FallbackCodepoint, FontStyle.Normal, FontWeight.Normal,
                    FontStretch.Normal, defaultFontFamily, null, out var fallbackTypeface));
                Assert.True(fontManager.TryGetGlyphTypeface(fallbackTypeface, out var fallbackGlyphTypeface));
                Assert.True(fallbackGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(' ', out _));

                var text = (char.ConvertFromUtf32(FallbackCodepoint) + " b").AsMemory();

                var textCharacters = new TextCharacters(text, defaultProperties);

                var results = FormattingObjectPool.Instance.TextRunLists.Rent();

                try
                {
                    TextRunProperties? previousProperties = null;

                    textCharacters.GetShapeableCharacters(text, 0, fontManager, ref previousProperties, results);

                    Assert.Equal(2, results.Count);

                    // The fallback run covers the Hebrew letter only. Before the fix it was 2 characters
                    // long: the space was pulled into the fallback run and rendered with its metrics.
                    Assert.Equal(1, results[0].Length);
                    Assert.Equal(fallbackTypeface, results[0].Properties!.Typeface);

                    // The space returns to the primary along with the rest of the text.
                    Assert.Equal(2, results[1].Length);
                    Assert.Equal(defaultProperties.Typeface, results[1].Properties!.Typeface);
                }
                finally
                {
                    FormattingObjectPool.RentedList<TextRun>? toReturn = results;
                    FormattingObjectPool.Instance.TextRunLists.Return(ref toReturn);
                }
            }
        }

        // The user-visible half of the same defect: the absorbed space is measured with the fallback
        // font, so a space typed after an emoji has a different advance than the same space elsewhere
        // in the line - a full em with the platform emoji fonts, and a narrower space with the emoji
        // font bundled here. Either way it is not the primary's.
        // https://github.com/AvaloniaUI/Avalonia/issues/14011
        [Fact]
        public void FormatLine_Keeps_A_Space_After_A_Fallback_Run_At_The_Primary_Width()
        {
            using (Start(PrimaryFont, EmojiFont))
            {
                var fontManager = FontManager.Current;

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var defaultGlyphTypeface = defaultProperties.CachedGlyphTypeface;

                Assert.False(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(EmojiCodepoint, out _));

                Assert.True(fontManager.TryMatchCharacter(EmojiCodepoint, FontStyle.Normal, FontWeight.Normal,
                    FontStretch.Normal, defaultProperties.Typeface.FontFamily, null, out var emojiTypeface));
                Assert.True(fontManager.TryGetGlyphTypeface(emojiTypeface, out var emojiGlyphTypeface));

                // The whole point of the test: the two fonts disagree about how wide a space is, so
                // whichever font shapes it is directly observable in the line width.
                Assert.NotEqual(SpaceAdvanceInEm(defaultGlyphTypeface), SpaceAdvanceInEm(emojiGlyphTypeface), 3);

                var formatter = new TextFormatterImpl();

                double Width(string text)
                {
                    var textLine = formatter.FormatLine(new SingleBufferTextSource(text, defaultProperties), 0,
                        double.PositiveInfinity, new GenericTextParagraphProperties(defaultProperties));

                    Assert.NotNull(textLine);

                    return textLine.WidthIncludingTrailingWhitespace;
                }

                var emoji = char.ConvertFromUtf32(EmojiCodepoint);

                // Isolate the space by differencing, so the surrounding glyphs' advances cancel out.
                var plainSpace = Width("a b") - Width("ab");
                var spaceAfterFallback = Width(emoji + " b") - Width(emoji + "b");

                Assert.Equal(plainSpace, spaceAfterFallback, 3);
            }
        }

        private static double SpaceAdvanceInEm(GlyphTypeface glyphTypeface)
        {
            Assert.True(glyphTypeface.CharacterToGlyphMap.TryGetGlyph(' ', out var glyph));
            Assert.True(glyphTypeface.TryGetHorizontalGlyphAdvance(glyph, out var advance));

            return (double)advance / glyphTypeface.Metrics.DesignEmHeight;
        }

        // The previous run's font is reused as an anti-thrashing bias. A space belongs to the primary
        // font, so it forms a run of its own between two fallback words - and that run must not become
        // the bias, or each word re-runs the fallback search and the two can land on different fonts.
        [Fact]
        public void GetShapeableCharacters_Keeps_The_Previous_Fallback_Across_A_Space()
        {
            using (Start(PrimaryFont, NotoSansScFont, NotoSansJpFont))
            {
                var fontManager = FontManager.Current;

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                // The previous run resolved to the Simplified-Chinese font.
                var scTypeface = new Typeface(new FontFamily("fonts:SystemFonts#Noto Sans SC"));
                Assert.True(fontManager.TryGetGlyphTypeface(scTypeface, out var scGlyphTypeface));

                const int han = 0x4E2D; // 中, covered by both regional fonts.

                // Preconditions: the primary covers the space but not the ideograph, the previous font
                // covers the ideograph, and a fresh search for it would pick the *other* font - so the
                // font of the second run tells us whether the bias survived the space.
                Assert.True(defaultProperties.CachedGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(' ', out _));
                Assert.False(defaultProperties.CachedGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(han, out _));
                Assert.True(scGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(han, out _));

                Assert.True(fontManager.TryMatchCharacter(han, FontStyle.Normal, FontWeight.Normal,
                    FontStretch.Normal, defaultProperties.Typeface.FontFamily, null, out var freshMatch));
                Assert.True(fontManager.TryGetGlyphTypeface(freshMatch, out var freshGlyphTypeface));
                Assert.Equal("Noto Sans JP", freshGlyphTypeface.FamilyName);

                var text = (" " + char.ConvertFromUtf32(han)).AsMemory();

                var textCharacters = new TextCharacters(text, defaultProperties);

                var results = FormattingObjectPool.Instance.TextRunLists.Rent();

                try
                {
                    TextRunProperties? previousProperties = new GenericTextRunProperties(scTypeface);

                    textCharacters.GetShapeableCharacters(text, 0, fontManager, ref previousProperties, results);

                    Assert.Equal(2, results.Count);

                    Assert.Equal(1, results[0].Length);
                    Assert.Equal(defaultProperties.Typeface, results[0].Properties!.Typeface);

                    Assert.True(fontManager.TryGetGlyphTypeface(results[1].Properties!.Typeface, out var runGlyphTypeface));
                    Assert.Equal("Noto Sans SC", runGlyphTypeface.FamilyName);
                }
                finally
                {
                    FormattingObjectPool.RentedList<TextRun>? toReturn = results;
                    FormattingObjectPool.Instance.TextRunLists.Return(ref toReturn);
                }
            }
        }

        // Only spacing whitespace (Zs) returns to the default typeface. Codepoint.IsWhiteSpace also
        // covers control and format codepoints - including the default-ignorable bidi controls, which
        // many fonts map. A default typeface that cannot shape the script must not pull a
        // right-to-left mark out of the fallback run just because its cmap has it: the mark renders
        // nothing either way, and splitting there cuts the run for no reason.
        [Fact]
        public void TryGetShapeableLength_Does_Not_Reclaim_A_Bidi_Control_As_Whitespace()
        {
            using (Start(PrimaryFont, FallbackFont))
            {
                // DejaVu Sans plays the default: its cmap has the Arabic letter, the right-to-left
                // mark and the space, but the test probes the tier where it cannot shape Arabic.
                // Cascadia Code plays the probed fallback; it has the letter and needs no glyph for
                // the default-ignorable mark.
                var defaultGlyphTypeface = new Typeface(FontFamily.Parse(
                    "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#DejaVu Sans")).GlyphTypeface;
                var probedGlyphTypeface = new Typeface(FontFamily.Parse(
                    "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Cascadia Code")).GlyphTypeface;

                const int alef = 0x0627;
                const int rightToLeftMark = 0x200F;

                Assert.True(probedGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(alef, out _));
                Assert.True(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(rightToLeftMark, out _));
                Assert.True(defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(' ', out _));

                // Letter, mark, letter, then a space: the mark stays inside the fallback run, the
                // space still returns to the default.
                var text = "ا‏ا z";

                Assert.True(TextCharacters.TryGetShapeableLength(text.AsSpan(), probedGlyphTypeface,
                    defaultGlyphTypeface, defaultCanShapeScript: false, requireFullCluster: true,
                    out var length));

                Assert.Equal(3, length);
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
