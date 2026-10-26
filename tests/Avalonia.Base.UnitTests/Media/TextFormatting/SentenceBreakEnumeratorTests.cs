using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class SentenceBreakEnumeratorTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public SentenceBreakEnumeratorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        // Conformance test: skipped by default because it downloads UCD data.
        // Enable by removing the Skip attribute when updating Unicode data.
        [Theory(Skip = "Only run when we update Unicode data.")]
        [ClassData(typeof(SentenceBreakTestDataGenerator))]
        public void ShouldFindBreaks(int lineNumber, int[] codePoints, int[] breakPoints, string rules)
        {
            var text = string.Join(null, codePoints.Select(char.ConvertFromUtf32));

            var enumerator = new SentenceBreakEnumerator(text);

            var foundBreaks = new List<int> { 0 };
            var currentPosition = 0;

            while (enumerator.MoveNext(out var segment))
            {
                var textSpan = segment.Text;
                while (!textSpan.IsEmpty)
                {
                    Codepoint.ReadAt(textSpan, 0, out var consumed);
                    textSpan = textSpan.Slice(consumed);
                    currentPosition++;
                }
                foundBreaks.Add(currentPosition);
            }

            var pass = foundBreaks.Count == breakPoints.Length;

            if (pass)
            {
                for (var i = 0; i < foundBreaks.Count; i++)
                {
                    if (foundBreaks[i] != breakPoints[i])
                    {
                        pass = false;
                        break;
                    }
                }
            }

            if (!pass)
            {
                _outputHelper.WriteLine($"Failed test on line {lineNumber}");
                _outputHelper.WriteLine("");
                _outputHelper.WriteLine($"    Code Points: {string.Join(" ", codePoints)}");
                _outputHelper.WriteLine($"Expected Breaks: {string.Join(" ", breakPoints)}");
                _outputHelper.WriteLine($"  Actual Breaks: {string.Join(" ", foundBreaks)}");
                _outputHelper.WriteLine($"           Text: {text}");
                _outputHelper.WriteLine($"     Char Props: {string.Join(" ", codePoints.Select(x => new Codepoint((uint)x).SentenceBreakClass))}");
                _outputHelper.WriteLine($"          Rules: {rules}");
                _outputHelper.WriteLine("");
            }

            Assert.True(pass);
        }

        // ── Targeted tests ────────────────────────────────────────────────────────

        [Fact]
        public void EmptyString_ReturnsNoSegments()
        {
            var enumerator = new SentenceBreakEnumerator(ReadOnlySpan<char>.Empty);
            Assert.False(enumerator.MoveNext(out _));
        }

        [Fact]
        public void SingleChar_ReturnsSingleSegment()
        {
            var enumerator = new SentenceBreakEnumerator("A".AsSpan());
            Assert.True(enumerator.MoveNext(out var seg));
            Assert.Equal(0, seg.Offset);
            Assert.Equal(1, seg.Text.Length);
            Assert.False(enumerator.MoveNext(out _));
        }

        [Fact]
        public void SimpleTerminator_SplitsAtSentenceBoundary()
        {
            // "Hello. World" → ["Hello. ", "World"]
            var text = "Hello. World";
            var segments = CollectSegments(text);
            Assert.Equal(2, segments.Count);
            Assert.Equal("Hello. ", segments[0].Text);
            Assert.Equal("World", segments[1].Text);
        }

        [Fact]
        public void ExclamationMark_SplitsAtSentenceBoundary()
        {
            var text = "Hello! World";
            var segments = CollectSegments(text);
            Assert.Equal(2, segments.Count);
            Assert.Equal("Hello! ", segments[0].Text);
            Assert.Equal("World", segments[1].Text);
        }

        [Fact]
        public void QuestionMark_SplitsAtSentenceBoundary()
        {
            var text = "What? Well";
            var segments = CollectSegments(text);
            Assert.Equal(2, segments.Count);
            Assert.Equal("What? ", segments[0].Text);
            Assert.Equal("Well", segments[1].Text);
        }

        [Theory]
        [InlineData("Hello. world")]   // SB8: ATerm × lowercase → no break
        [InlineData("Hello.  world")] // SB8: ATerm Sp* × lowercase → no break
        public void ATermBeforeLower_NoBreak(string text)
        {
            // According to SB8, a period followed by lowercase should NOT split.
            var segments = CollectSegments(text);
            Assert.Equal(1, segments.Count);
        }

        [Fact]
        public void Decimal_NoBreak()
        {
            // SB6: ATerm × Numeric → no break (2.5 stays together)
            var text = "The value is 2.5 meters.";
            var segments = CollectSegments(text);
            // Should be 1 sentence (the decimal point is not a sentence terminator)
            Assert.Equal(1, segments.Count);
        }

        [Fact]
        public void Abbreviation_SB7_NoBreak()
        {
            // SB7: (Upper | Lower) ATerm × Upper → no break
            // "U.S" — Upper (U) ATerm (.) Upper (S) must NOT break between '.' and 'S'.
            // Use "U.S. Treasury" where "Treasury" starts with Upper, so SB8 does NOT suppress
            // (SB8 requires a subsequent Lower; "Treasury" starts Upper which is a SB8 blocker).
            var text = "U.S. Treasury has funds.";
            var segments = CollectSegments(text);
            // "U.S." followed by space and Upper ("Treasury") → SB11 fires → break before "Treasury"
            // The important thing: no spurious break between 'U', '.', 'S', '.' inside "U.S."
            Assert.Equal(2, segments.Count);
            Assert.StartsWith("U.S.", segments[0].Text);
        }

        [Fact]
        public void AbbreviationFollowedByLower_SB8_NoBreak()
        {
            // SB8: ATerm Close* Sp* × (¬(OLetter|Upper|Lower|ParaSep|SATerm))* Lower
            // "U.S.A. has" — ATerm + Sp + Lower("h") → SB8 suppresses the break.
            // Per UAX-29 this is ONE sentence.
            var text = "U.S.A. has";
            var segments = CollectSegments(text);
            Assert.Equal(1, segments.Count);
        }

        [Fact]
        public void ParagraphSeparator_Sep_SplitsImmediately()
        {
            // U+2029 PARAGRAPH SEPARATOR is SentenceBreakClass.Sep → SB4 break after it
            var text = "Hello\u2029World";
            var segments = CollectSegments(text);
            Assert.Equal(2, segments.Count);
            Assert.Equal("Hello\u2029", segments[0].Text);
            Assert.Equal("World", segments[1].Text);
        }

        [Fact]
        public void CrLf_TreatedAsSingleBreak()
        {
            // SB3: CR × LF (no break between them); SB4: break after LF
            var text = "Hello\r\nWorld";
            var segments = CollectSegments(text);
            Assert.Equal(2, segments.Count);
            Assert.Equal("Hello\r\n", segments[0].Text);
            Assert.Equal("World", segments[1].Text);
        }

        [Fact]
        public void SB9_CloseAndSpaceStayWithTerminator()
        {
            // SB9: (STerm | ATerm) Close* × (Close | Sp | Sep | CR | LF)
            // The closing quote and trailing space should stay with the sentence terminator.
            var text = "He said \"Hello.\" She replied.";
            var segments = CollectSegments(text);
            // There are 2 sentences; the closing quote stays with the first.
            Assert.Equal(2, segments.Count);
            Assert.Contains("Hello.\"", segments[0].Text);
        }

        [Fact]
        public void SurrogatePair_HandledCorrectly()
        {
            // 𝄞 (U+1D11E MUSICAL SYMBOL G CLEF) is encoded as a surrogate pair.
            var text = "𝄞. Hi";
            var segments = CollectSegments(text);
            // Should produce 2 segments; the surrogate pair is one codepoint
            Assert.Equal(2, segments.Count);
        }

        [Fact]
        public void AllSegmentsCoverWholeText()
        {
            var text = "First sentence. Second sentence! Third?";
            var enumerator = new SentenceBreakEnumerator(text.AsSpan());
            var covered = 0;

            while (enumerator.MoveNext(out var segment))
            {
                Assert.Equal(covered, segment.Offset);
                covered += segment.Text.Length;
            }

            Assert.Equal(text.Length, covered);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private readonly record struct SegmentInfo(int Offset, string Text);

        private static List<SegmentInfo> CollectSegments(string text)
        {
            var result = new List<SegmentInfo>();
            var enumerator = new SentenceBreakEnumerator(text.AsSpan());
            while (enumerator.MoveNext(out var segment))
            {
                result.Add(new SegmentInfo(segment.Offset, segment.Text.ToString()));
            }
            return result;
        }
    }
}
