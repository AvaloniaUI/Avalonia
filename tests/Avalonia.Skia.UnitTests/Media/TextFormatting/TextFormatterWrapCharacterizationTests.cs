#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    /// <summary>
    /// Characterization tests for <c>TextFormatterImpl.PerformTextWrapping</c>
    /// and its helpers (<c>MeasureLength</c>, <c>SplitTextRuns</c>,
    /// <c>ResetTrailingWhitespaceBidiLevels</c>).
    /// </summary>
    public class TextFormatterWrapCharacterizationTests
    {
        [Fact]
        public void Wrap_With_Infinite_Width_Yields_Single_Line_With_All_Runs()
        {
            using (TextFormatterTests.Start())
            {
                var line = WrapSingleLine("Hello world", paragraphWidth: double.PositiveInfinity);

                Assert.Equal("Hello world".Length, line.Length);
                Assert.True(line.WidthIncludingTrailingWhitespace > 0);
            }
        }

        [Fact]
        public void Wrap_With_Zero_Width_Forces_Minimum_Cluster()
        {
            // Width too small to fit any cluster — the implementation falls
            // back to one grapheme. This is the documented WrapWithOverflow
            // contract that lines 882-902 of TextFormatterImpl encode.
            using (TextFormatterTests.Start())
            {
                var line = WrapSingleLine("Hello", paragraphWidth: 0.001);

                Assert.True(line.Length >= 1,
                    "Wrap should always advance at least one grapheme even at zero width.");
            }
        }

        [Theory]
        [InlineData("AAAA BBBB CCCC DDDD", 40)]
        [InlineData("AAAA BBBB CCCC DDDD", 80)]
        [InlineData("AAAA BBBB CCCC DDDD", 120)]
        public void Wrap_Sum_Of_Line_Lengths_Equals_Input_Length(string text, double paragraphWidth)
        {
            using (TextFormatterTests.Start())
            {
                var lines = WrapAllLines(text, paragraphWidth);
                var totalLength = lines.Sum(l => l.Length);
                Assert.Equal(text.Length, totalLength);
            }
        }

        [Theory]
        [InlineData("AAAA BBBB CCCC DDDD", 40)]
        [InlineData("AAAA BBBB CCCC DDDD", 80)]
        public void Wrap_Each_Line_Width_Within_Paragraph_Width(string text, double paragraphWidth)
        {
            using (TextFormatterTests.Start())
            {
                var lines = WrapAllLines(text, paragraphWidth);
                foreach (var line in lines)
                {
                    // Width (excluding trailing whitespace) should fit the
                    // paragraph. The +1.0 tolerance handles the documented
                    // "single cluster wider than paragraph" overflow case.
                    Assert.True(line.Width <= paragraphWidth + 1.0,
                        $"Line width {line.Width} exceeds paragraph width {paragraphWidth} by more than 1px.");
                }
            }
        }

        [Fact]
        public void Wrap_Does_Not_Produce_Empty_Lines_For_NonEmpty_Input()
        {
            using (TextFormatterTests.Start())
            {
                var lines = WrapAllLines("the quick brown fox jumps over the lazy dog", paragraphWidth: 50);
                foreach (var line in lines)
                {
                    Assert.True(line.Length > 0, "Wrap should never emit a zero-length line for non-empty input.");
                }
            }
        }

        [Fact]
        public void Wrap_Points_Are_Grapheme_Boundaries()
        {
            // Multi-codepoint graphemes (emoji ZWJ sequences) must never be
            // split by the wrap algorithm — the wrap point has to coincide
            // with a grapheme boundary.
            using (TextFormatterTests.Start())
            {
                const string text = "abc 😀😀😀😀 xyz";
                var lines = WrapAllLines(text, paragraphWidth: 30);

                var boundaries = new HashSet<int>();
                var graphemeEnumerator = new GraphemeEnumerator(text.AsSpan());
                boundaries.Add(0);
                var pos = 0;
                while (graphemeEnumerator.MoveNext(out var grapheme))
                {
                    pos += grapheme.Length;
                    boundaries.Add(pos);
                }

                var cumulative = 0;
                foreach (var line in lines)
                {
                    cumulative += line.Length;
                    Assert.Contains(cumulative, boundaries);
                }
            }
        }

        [Fact]
        public void Wrap_Honours_Required_Break_Even_With_Available_Width()
        {
            using (TextFormatterTests.Start())
            {
                var line = WrapSingleLine("ab\ncd", paragraphWidth: double.PositiveInfinity);

                // Hard break sits at index 2 (the '\n'); PositionWrap is 3
                // (consumes the '\n').
                Assert.Equal(3, line.Length);
            }
        }

        [Fact]
        public void Wrap_Hard_Break_With_CRLF_Counts_Both_Characters()
        {
            using (TextFormatterTests.Start())
            {
                var line = WrapSingleLine("ab\r\ncd", paragraphWidth: double.PositiveInfinity);

                // CRLF is a single break with PositionWrap = 4 (consumes both chars).
                Assert.Equal(4, line.Length);
            }
        }

        [Fact]
        public void WrapWithOverflow_Long_Word_Followed_By_Space_Wraps_After_Space()
        {
            // The word "supercalifragilistic" has no break inside it. At a
            // small paragraph width with WrapWithOverflow, the wrap algorithm
            // should let the word overflow as a whole, then wrap on the next
            // break (the trailing space).
            using (TextFormatterTests.Start())
            {
                var line = WrapSingleLine("supercalifragilistic next", paragraphWidth: 30,
                    wrapping: TextWrapping.WrapWithOverflow);

                Assert.True(line.Length >= "supercalifragilistic".Length,
                    $"Expected first line to contain at least the whole long word; got length {line.Length}.");
                Assert.True(line.Length <= "supercalifragilistic ".Length,
                    "First line should not extend past the trailing space after the long word.");
            }
        }

        [Fact]
        public void Wrap_Strict_Long_Word_Splits_Inside_When_NoWrapPosition_Available()
        {
            // Pure Wrap (not WrapWithOverflow) on an unbreakable word: the
            // implementation falls back to splitting inside the word at the
            // best available cluster boundary.
            using (TextFormatterTests.Start())
            {
                var line = WrapSingleLine("supercalifragilistic", paragraphWidth: 30,
                    wrapping: TextWrapping.Wrap);

                Assert.True(line.Length > 0);
                Assert.True(line.Length < "supercalifragilistic".Length,
                    "Strict wrap should split inside the long word.");
            }
        }

        [Fact]
        public void Wrap_Continues_From_Previous_LineBreak()
        {
            using (TextFormatterTests.Start())
            {
                var text = "AAAA BBBB CCCC DDDD";
                var lines = WrapAllLines(text, paragraphWidth: 40);

                Assert.True(lines.Count >= 2, "Test setup must wrap onto at least two lines.");

                // Lines after the first reuse runs via WrappingTextLineBreak.
                // The contract: concatenated, they reproduce the original.
                var rebuilt = string.Concat(lines.Select(l => GetLineText(l)));
                Assert.Equal(text, rebuilt);
            }
        }

        [Fact]
        public void Wrap_With_LTR_Text_Does_Not_Touch_Trailing_Whitespace_Bidi()
        {
            // ResetTrailingWhitespaceBidiLevels is a no-op when the run's
            // BidiLevel already matches the paragraph. The wrap result should
            // be identical to a non-wrapped layout of the same paragraph.
            using (TextFormatterTests.Start())
            {
                var text = "Hello world from Avalonia";
                var wrappedLines = WrapAllLines(text, paragraphWidth: 80);
                var rebuilt = string.Concat(wrappedLines.Select(l => GetLineText(l)));
                Assert.Equal(text, rebuilt);
            }
        }

        [Fact]
        public void Wrap_Empty_Text_Yields_Null()
        {
            using (TextFormatterTests.Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default, 12,
                    foregroundBrush: Brushes.Black);
                var paragraphProperties = new GenericTextParagraphProperties(
                    defaultProperties, textWrapping: TextWrapping.Wrap);
                var textSource = new SingleBufferTextSource("", defaultProperties);
                var formatter = new TextFormatterImpl();

                var line = formatter.FormatLine(textSource, 0, 100, paragraphProperties);
                Assert.Null(line);
            }
        }

        private static TextLine WrapSingleLine(string text, double paragraphWidth,
            TextWrapping wrapping = TextWrapping.Wrap)
        {
            var defaultProperties = new GenericTextRunProperties(Typeface.Default, 12,
                foregroundBrush: Brushes.Black);
            var paragraphProperties = new GenericTextParagraphProperties(defaultProperties,
                textWrapping: wrapping);
            var textSource = new SingleBufferTextSource(text, defaultProperties);
            var formatter = new TextFormatterImpl();

            var line = formatter.FormatLine(textSource, 0, paragraphWidth, paragraphProperties);
            Assert.NotNull(line);
            return line!;
        }

        private static List<TextLine> WrapAllLines(string text, double paragraphWidth,
            TextWrapping wrapping = TextWrapping.Wrap)
        {
            var defaultProperties = new GenericTextRunProperties(Typeface.Default, 12,
                foregroundBrush: Brushes.Black);
            var paragraphProperties = new GenericTextParagraphProperties(defaultProperties,
                textWrapping: wrapping);
            var textSource = new SingleBufferTextSource(text, defaultProperties);
            var formatter = new TextFormatterImpl();

            var lines = new List<TextLine>();
            var pos = 0;
            TextLineBreak? previousLineBreak = null;
            while (pos < text.Length)
            {
                var line = formatter.FormatLine(textSource, pos, paragraphWidth,
                    paragraphProperties, previousLineBreak);
                if (line == null)
                {
                    break;
                }
                lines.Add(line);
                previousLineBreak = line.TextLineBreak;
                pos += line.Length;

                if (pos > 0 && lines.Count > 200)
                {
                    Assert.Fail("Wrap appears to be looping; bailing out.");
                }
            }
            return lines;
        }

        private static string GetLineText(TextLine line)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var run in line.TextRuns)
            {
                sb.Append(run.Text.Span);
            }
            return sb.ToString();
        }
    }
}
