#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    /// <summary>
    /// Characterization tests for <see cref="TextCollapsingProperties"/>
    /// implementations, with emphasis on BiDi correctness. Pins current
    /// behavior before the fixes described in
    /// <c>planning/text-collapsing-bidi-plan.md</c>.
    ///
    /// Conventions:
    ///   * Tests that pass today are <c>[Fact]</c> — they protect against
    ///     regressions from the upcoming refactor.
    ///   * Tests that document a known bug carry <c>[Fact(Skip = "Bx: …")]</c>
    ///     so the suite stays green; remove the <c>Skip</c> when the
    ///     corresponding fix lands.
    ///   * Assertions target invariants (content preserved, ellipsis present,
    ///     prefix preserved, etc.) rather than exact glyph output so they
    ///     survive font changes.
    /// </summary>
    public class TextCollapsingBidiTests
    {
        // --- LTR sanity (regression guards) -----------------------------------

        [Fact]
        public void Ltr_TrailingCharacter_Trims_From_End()
        {
            using (TextFormatterTests.Start())
            {
                var line = BuildLine("Hello world", FlowDirection.LeftToRight);
                var collapsing = TrailingChar(line.Width / 2, FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var text = LogicalText(collapsed);
                Assert.Contains("…", text);
                Assert.StartsWith("H", text);
            }
        }

        [Fact]
        public void Ltr_TrailingWord_Trims_On_Word_Boundary()
        {
            using (TextFormatterTests.Start())
            {
                var line = BuildLine("Hello world foo", FlowDirection.LeftToRight);
                var collapsing = TrailingWord(line.Width / 2, FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                Assert.Contains("…", LogicalText(collapsed));
            }
        }

        [Fact]
        public void Ltr_PrefixCharacterEllipsis_Preserves_Prefix_And_Suffix()
        {
            // Matches the existing Should_Collapse_Line LTR baseline:
            //   "01234 01234 01234" @ width=120, prefixLength=8 → "01234 01…4 01234"
            using (TextFormatterTests.Start())
            {
                var line = BuildLine("01234 01234 01234", FlowDirection.LeftToRight);
                var collapsing = LeadingPrefix(prefixLength: 8, width: 120.0, FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var text = LogicalText(collapsed);
                Assert.StartsWith("01234 01", text);
                Assert.Contains("…", text);
                // Suffix must reappear after the symbol.
                Assert.EndsWith("4 01234", text);
            }
        }

        [Fact]
        public void Ltr_PathSegmentEllipsis_Collapses_Middle()
        {
            using (TextFormatterTests.Start())
            {
                var line = BuildLine("verylongdirectory\\file.txt", FlowDirection.LeftToRight);
                var collapsing = new TextPathSegmentEllipsis(
                    "…", line.Width / 2,
                    new GenericTextRunProperties(Typeface.Default),
                    FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var text = LogicalText(collapsed);
                Assert.Contains("…", text);
                // Last segment ("file.txt") should be preserved on at least
                // some prefix; we don't assert exact width because Width math
                // depends on the font.
                Assert.Contains(".txt", text);
            }
        }

        // --- Width edges -------------------------------------------------------

        [Fact]
        public void Width_Greater_Than_Line_Returns_Same_Line()
        {
            using (TextFormatterTests.Start())
            {
                var line = BuildLine("abc", FlowDirection.LeftToRight);
                var collapsing = TrailingChar(line.Width + 100, FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                // Collapse returns null → TextLineImpl.Collapse returns `this`.
                Assert.Same(line, collapsed);
                Assert.False(collapsed.HasCollapsed);
            }
        }

        [Fact]
        public void Width_Less_Than_Symbol_Returns_Empty_Collapsed_Line()
        {
            using (TextFormatterTests.Start())
            {
                var line = BuildLine("abcdef", FlowDirection.LeftToRight);

                // Width below symbol width → implementation returns [] → line
                // gets HasCollapsed = true but no runs.
                var collapsing = TrailingChar(width: 0.001, FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                Assert.True(collapsed.HasCollapsed);
                Assert.Empty(collapsed.TextRuns);
            }
        }

        // --- RTL paragraph -----------------------------------------------------
        // Per the plan: trimming should happen in LOGICAL order. The consumer
        // (TextLineImpl.Collapse → FinalizeLine → BidiReorderer) handles the
        // visual reordering. So for an RTL paragraph, the logical prefix
        // (start of the original string) must be preserved by trailing-*
        // ellipsis, and the ellipsis symbol must appear in the output.

        [Fact]
        public void Rtl_TrailingCharacter_Preserves_Logical_Prefix()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "السلام عليكم ورحمة الله وبركاته";
                var line = BuildLine(text, FlowDirection.RightToLeft);
                var collapsing = TrailingChar(line.Width / 2, FlowDirection.RightToLeft);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var logical = LogicalText(collapsed);
                Assert.Contains("…", logical);
                Assert.StartsWith(text.Substring(0, 1), logical);
            }
        }

        [Fact]
        public void Rtl_TrailingWord_Preserves_Logical_Prefix()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "السلام عليكم ورحمة الله وبركاته";
                var line = BuildLine(text, FlowDirection.RightToLeft);
                var collapsing = TrailingWord(line.Width / 2, FlowDirection.RightToLeft);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                Assert.Contains("…", LogicalText(collapsed));
            }
        }

        // Note: this passes today because a single-run RTL line has visual
        // order == logical order, so the B2 visual-iteration bug doesn't
        // manifest. The multi-run B2 case is covered by
        // Mixed_PrefixCharacterEllipsis_Preserves_Logical_Prefix_And_Suffix.
        [Fact]
        public void Rtl_PrefixCharacterEllipsis_Preserves_Logical_Prefix()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "السلام عليكم ورحمة الله وبركاته";
                var line = BuildLine(text, FlowDirection.RightToLeft);
                var collapsing = LeadingPrefix(prefixLength: 4, width: line.Width / 2, FlowDirection.RightToLeft);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var logical = LogicalText(collapsed);
                Assert.StartsWith(text.Substring(0, 4), logical);
                Assert.Contains("…", logical);
            }
        }

        // --- Mixed bidi --------------------------------------------------------

        [Fact]
        public void Mixed_TrailingCharacter_Preserves_Logical_Prefix()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "Hello مرحبا world";
                var line = BuildLine(text, FlowDirection.LeftToRight);
                var collapsing = TrailingChar(line.Width * 0.6, FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                Assert.StartsWith("Hello", LogicalText(collapsed));
            }
        }

        [Fact]
        public void Mixed_PrefixCharacterEllipsis_Preserves_Logical_Prefix_And_Suffix()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "Hello مرحبا world";
                var line = BuildLine(text, FlowDirection.LeftToRight);
                var collapsing = LeadingPrefix(prefixLength: 5, width: line.Width * 0.6, FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var logical = LogicalText(collapsed);
                Assert.StartsWith("Hello", logical);
                Assert.Contains("…", logical);
            }
        }

        // --- Phase 3: TextEllipsisHelper + TextPathSegmentEllipsis BiDi -------
        // These classes were already structured correctly (both use
        // LogicalTextRunEnumerator) but the original test surface only had LTR
        // cases. The tests below pin BiDi behavior so future refactors can't
        // silently break it.

        [Fact]
        public void Mixed_TrailingWord_Preserves_Logical_Prefix()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "Hello مرحبا world";
                var line = BuildLine(text, FlowDirection.LeftToRight);
                var collapsing = TrailingWord(line.Width * 0.6, FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var logical = LogicalText(collapsed);
                Assert.Contains("…", logical);
                Assert.StartsWith("Hello", logical);
            }
        }

        [Fact]
        public void Mixed_PathSegmentEllipsis_Preserves_Last_Segment()
        {
            using (TextFormatterTests.Start())
            {
                // Mixed-bidi path: ASCII-only separators with an RTL directory
                // name embedded. Segmentation is separator-driven, so the
                // logical-tail segment ("file.txt") must survive.
                const string text = "C:\\folder\\مجلد\\file.txt";
                var line = BuildLine(text, FlowDirection.LeftToRight);
                var collapsing = new TextPathSegmentEllipsis(
                    "…", line.Width / 2,
                    new GenericTextRunProperties(Typeface.Default),
                    FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var logical = LogicalText(collapsed);
                Assert.Contains("…", logical);
                Assert.Contains("file.txt", logical);
            }
        }

        [Fact]
        public void Rtl_PathSegmentEllipsis_Preserves_Last_Segment()
        {
            using (TextFormatterTests.Start())
            {
                // Pure-RTL path. Avalonia's font fallback may render Arabic as
                // .notdef glyphs in the test environment, but segmentation is
                // character-driven (separators are ASCII '/' and '\\') so the
                // logical-tail segment "ملف.txt" must still be detected and
                // preserved.
                const string text = "مجلد/مجلد2/ملف.txt";
                var line = BuildLine(text, FlowDirection.RightToLeft);
                var collapsing = new TextPathSegmentEllipsis(
                    "…", line.Width / 2,
                    new GenericTextRunProperties(Typeface.Default),
                    FlowDirection.RightToLeft);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var logical = LogicalText(collapsed);
                Assert.Contains("…", logical);
                Assert.Contains("ملف.txt", logical);
            }
        }

        // --- Middle-collapse coverage (distinguishes the main path from the
        //     "trim from start" fallback). For an LTR path with the
        //     middle segment large enough to make a single-segment collapse
        //     bring the line under budget, the result should keep BOTH
        //     the first and last segments. The fallback would drop the
        //     first segments.

        [Fact]
        public void Ltr_PathSegmentEllipsis_Middle_Collapse_Preserves_First_And_Last_Segments()
        {
            using (TextFormatterTests.Start())
            {
                // 3-segment path; middle is intentionally long so collapsing
                // it alone produces a fitting result.
                const string text = "a/middlemiddlemiddlemiddlemiddlemiddlemiddlemiddlemiddlemiddle/c.txt";
                var line = BuildLine(text, FlowDirection.LeftToRight);
                var budget = line.Width * 0.3;
                var collapsing = new TextPathSegmentEllipsis(
                    "…", budget,
                    new GenericTextRunProperties(Typeface.Default),
                    FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var logical = LogicalText(collapsed);
                Assert.Contains("…", logical);
                Assert.Contains("a", logical);
                Assert.Contains("c.txt", logical);
            }
        }

        [Fact]
        public void Rtl_PathSegmentEllipsis_Middle_Collapse_Preserves_First_And_Last_Segments()
        {
            // B8: TextPathSegmentEllipsis.MeasureSegmentWidth assumes the
            // ShapedBuffer's GlyphCluster values increase monotonically with
            // glyph index. That holds for LTR buffers but NOT for RTL — RTL
            // buffers have glyphs in visual order with cluster values
            // DECREASING. The result: any RTL segment's measured width comes
            // out close to a single glyph's advance, never close to its real
            // width. The middle-collapse algorithm then can't find a window
            // whose collapse-savings make the line fit, and falls back to
            // "trim from start" — which drops the logical-first segment.
            using (TextFormatterTests.Start())
            {
                const string text = "اول/منتصفمنتصفمنتصفمنتصفمنتصفمنتصفمنتصفمنتصف/اخر.txt";
                var line = BuildLine(text, FlowDirection.RightToLeft);
                var budget = line.Width * 0.3;
                var collapsing = new TextPathSegmentEllipsis(
                    "…", budget,
                    new GenericTextRunProperties(Typeface.Default),
                    FlowDirection.RightToLeft);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var logical = LogicalText(collapsed);
                Assert.Contains("…", logical);
                Assert.Contains("اول", logical);
                Assert.Contains("اخر.txt", logical);
            }
        }

        // --- B9: TryMeasureCharacters/Backwards bidi contract ----------------
        // These two methods walk the ShapedBuffer in visual order. For LTR the
        // visual order equals the logical order, so the returned "length"
        // (counted via cluster deltas) is the logical-leading / logical-trailing
        // char count that callers want. For RTL the visual order is the
        // logical-REVERSE order, so the cumulative width is measured for the
        // wrong set of characters even though the COUNT happens to come out
        // right for uniform-width fonts. The contract callers depend on:
        //   * TryMeasureCharacters(budget) → largest N such that the LOGICAL
        //     leading N characters fit within `budget`.
        //   * TryMeasureCharactersBackwards(budget) → largest N such that the
        //     LOGICAL trailing N characters fit within `budget`.
        // We verify each by cross-checking the returned length against the
        // cluster-cache-based logical width measurement.

        [Theory]
        [InlineData("Hello world abcdef", true)]
        [InlineData("السلام عليكم ورحمة", false)]
        public void TryMeasureCharacters_Returned_Length_Fits_Logical_Leading_In_Budget(string text, bool ltr)
        {
            using (TextFormatterTests.Start())
            {
                var dir = ltr ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
                var line = BuildLine(text, dir);
                var shapedRun = line.TextRuns.OfType<ShapedTextRun>().FirstOrDefault();
                Assert.NotNull(shapedRun);

                var buffer = shapedRun!.ShapedBuffer;
                var totalWidth = shapedRun.Size.Width;

                // Probe at several budget points; the contract must hold at all of them.
                for (var i = 1; i < 10; i++)
                {
                    var budget = totalWidth * i / 10;
                    if (!shapedRun.TryMeasureCharacters(budget, out var measured) || measured <= 0)
                    {
                        continue;
                    }

                    // The width of the LOGICAL leading `measured` characters must fit
                    // in `budget`. GetCharRangeWidth uses the cluster cache, which is
                    // built in logical order for both directions.
                    var actualLeadingWidth = buffer.GetCharRangeWidth(0, measured);

                    Assert.True(actualLeadingWidth <= budget + 0.5,
                        $"{dir}: budget={budget:F2}, measured={measured}, " +
                        $"actual logical-leading width={actualLeadingWidth:F2}");
                }
            }
        }

        [Theory]
        [InlineData("Hello world abcdef", true)]
        [InlineData("السلام عليكم ورحمة", false)]
        public void TryMeasureCharactersBackwards_Returned_Length_Fits_Logical_Trailing_In_Budget(string text, bool ltr)
        {
            using (TextFormatterTests.Start())
            {
                var dir = ltr ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
                var line = BuildLine(text, dir);
                var shapedRun = line.TextRuns.OfType<ShapedTextRun>().FirstOrDefault();
                Assert.NotNull(shapedRun);

                var buffer = shapedRun!.ShapedBuffer;
                var totalWidth = shapedRun.Size.Width;
                var textLength = shapedRun.Length;

                for (var i = 1; i < 10; i++)
                {
                    var budget = totalWidth * i / 10;
                    if (!shapedRun.TryMeasureCharactersBackwards(budget, out var measured, out _) || measured <= 0)
                    {
                        continue;
                    }

                    var actualTrailingWidth = buffer.GetCharRangeWidth(textLength - measured, textLength);

                    Assert.True(actualTrailingWidth <= budget + 0.5,
                        $"{dir}: budget={budget:F2}, measured={measured}, " +
                        $"actual logical-trailing width={actualTrailingWidth:F2}");
                }
            }
        }

        // --- B1: LogicalTextRunEnumerator ------------------------------------

        [Fact]
        public void LogicalTextRunEnumerator_Without_IndexedRuns_Returns_Distinct_Runs()
        {
            using (TextFormatterTests.Start())
            {
                var props = new GenericTextRunProperties(Typeface.Default);
                var runs = new TextRun[]
                {
                    new TextCharacters("AAA", props),
                    new TextCharacters("BBB", props),
                    new TextCharacters("CCC", props),
                };

                // Construct TextLineImpl directly and SKIP FinalizeLine so that
                // _indexedTextRuns stays null. This is exactly the branch
                // LogicalTextRunEnumerator handles incorrectly today.
                var paragraphProps = new GenericTextParagraphProperties(props);
                var line = new TextLineImpl(runs, 0, 9, double.PositiveInfinity, paragraphProps);

                var enumerator = new LogicalTextRunEnumerator(line);
                var seen = new List<TextRun>();
                while (enumerator.MoveNext(out var run))
                {
                    seen.Add(run!);
                }

                Assert.Equal(3, seen.Count);
                Assert.Same(runs[0], seen[0]);
                Assert.Same(runs[1], seen[1]);
                Assert.Same(runs[2], seen[2]);
            }
        }

        // --- B3: TextLeadingPrefixCharacterEllipsis constructor validation ----

        [Fact]
        public void LeadingPrefix_Negative_PrefixLength_Throws()
        {
            using (TextFormatterTests.Start())
            {
                var props = new GenericTextRunProperties(Typeface.Default);
                Assert.Throws<System.ArgumentOutOfRangeException>(
                    () => new TextLeadingPrefixCharacterEllipsis(
                        "…", prefixLength: -1, width: 100, props, FlowDirection.LeftToRight));
            }
        }

        // --- B4: TextLeadingPrefixCharacterEllipsis honours FlowDirection -----

        [Fact]
        public void LeadingPrefix_Honours_FlowDirection_For_Symbol()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "السلام عليكم ورحمة";
                var line = BuildLine(text, FlowDirection.RightToLeft);
                var collapsing = LeadingPrefix(prefixLength: 4, width: line.Width / 2, FlowDirection.RightToLeft);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);

                // The ellipsis symbol's run should pick up the RTL bidi level
                // from the FlowDirection passed to the constructor. Today the
                // ctor in Collapse() hardcodes LeftToRight, so the symbol run
                // has IsLeftToRight == true.
                var ellipsisRun = collapsed.TextRuns
                    .OfType<ShapedTextRun>()
                    .FirstOrDefault(r => r.Text.ToString().Contains("…"));
                Assert.NotNull(ellipsisRun);
                Assert.False(ellipsisRun!.ShapedBuffer.IsLeftToRight);
            }
        }

        // --- Mixed shaped + drawable / multi-run -------------------------------

        [Fact]
        public void Collapse_With_Multiple_Shaped_Runs_Preserves_Ellipsis()
        {
            // Three independent runs via FixedRunsTextSource. Trim point lands
            // somewhere in the middle — collapse must not silently drop a run
            // or duplicate one (covers the SplitTextRuns interaction).
            using (TextFormatterTests.Start())
            {
                var props = new GenericTextRunProperties(Typeface.Default);
                var sourceRuns = new TextRun[]
                {
                    new TextCharacters("AAAA", props),
                    new TextCharacters("BBBB", props),
                    new TextCharacters("CCCC", props),
                };
                var src = new FixedRunsTextSource(sourceRuns);
                var formatter = new TextFormatterImpl();
                var line = formatter.FormatLine(src, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(props));
                Assert.NotNull(line);

                var collapsing = TrailingChar(line!.Width / 2, FlowDirection.LeftToRight);
                var collapsed = line.Collapse(collapsing);

                AssertCollapsed(collapsed, line);
                var text = LogicalText(collapsed);
                Assert.Contains("…", text);
                // Every preserved character must come from the original source
                // text in original order — no garbage.
                var preserved = text.Replace("…", string.Empty);
                Assert.StartsWith(preserved, "AAAABBBBCCCC");
            }
        }

        // --- Helpers -----------------------------------------------------------

        private static TextLine BuildLine(string text, FlowDirection flow)
        {
            var props = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
            var paragraphProps = new GenericTextParagraphProperties(
                flow, TextAlignment.Left, true, true, props, TextWrapping.NoWrap, 0, 0, 0);
            var source = new SingleBufferTextSource(text, props);
            var formatter = new TextFormatterImpl();
            var line = formatter.FormatLine(source, 0, double.PositiveInfinity, paragraphProps);
            Assert.NotNull(line);
            return line!;
        }

        private static TextTrailingCharacterEllipsis TrailingChar(double width, FlowDirection flow)
            => new("…", width, new GenericTextRunProperties(Typeface.Default), flow);

        private static TextTrailingWordEllipsis TrailingWord(double width, FlowDirection flow)
            => new("…", width, new GenericTextRunProperties(Typeface.Default), flow);

        private static TextLeadingPrefixCharacterEllipsis LeadingPrefix(
            int prefixLength, double width, FlowDirection flow)
            => new("…", prefixLength, width,
                new GenericTextRunProperties(Typeface.Default), flow);

        private static void AssertCollapsed(TextLine collapsed, TextLine original)
        {
            Assert.NotSame(original, collapsed);
            Assert.True(collapsed.HasCollapsed,
                "Collapsed line must report HasCollapsed = true.");
        }

        /// <summary>
        /// Concatenates run text in logical order via
        /// <see cref="LogicalTextRunEnumerator"/>. For LTR-only lines this
        /// equals walking <c>TextRuns</c> directly; for RTL/mixed lines it
        /// returns the original-text order (what the collapse contract
        /// requires) instead of the visual post-bidi order.
        /// </summary>
        private static string LogicalText(TextLine line)
        {
            var enumerator = new LogicalTextRunEnumerator(line);
            var sb = new StringBuilder();
            while (enumerator.MoveNext(out var run))
            {
                sb.Append(run!.Text.Span);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Local copy of the FixedRunsTextSource pattern used in
        /// TextLineTests — that class is private, so duplicate here.
        /// </summary>
        private sealed class FixedRunsTextSource : ITextSource
        {
            private readonly IReadOnlyList<TextRun> _textRuns;

            public FixedRunsTextSource(IReadOnlyList<TextRun> textRuns)
            {
                _textRuns = textRuns;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                var pos = 0;
                foreach (var run in _textRuns)
                {
                    if (pos == textSourceIndex)
                    {
                        return run;
                    }
                    pos += run.Length;
                }
                return null;
            }
        }
    }
}
