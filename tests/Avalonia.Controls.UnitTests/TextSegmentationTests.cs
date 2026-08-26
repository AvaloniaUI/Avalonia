using System;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextSegmentationTests : ScopedTestBase
    {
        [Fact]
        public void EmptyString_ReturnsZeroZero()
        {
            var (start, end) = TextSegmentation.SentenceBounds(0, ReadOnlySpan<char>.Empty);
            Assert.Equal(0, start);
            Assert.Equal(0, end);
        }

        [Fact]
        public void SimpleSentence_FirstSegment()
        {
            var text = "Hello. World";
            var (start, end) = TextSegmentation.SentenceBounds(0, text.AsSpan());
            Assert.Equal(0, start);
            Assert.Equal(7, end); // "Hello. "
        }

        [Fact]
        public void SimpleSentence_SecondSegment()
        {
            var text = "Hello. World";
            var (start, end) = TextSegmentation.SentenceBounds(7, text.AsSpan());
            Assert.Equal(7, start);
            Assert.Equal(text.Length, end);
        }

        [Fact]
        public void OffsetInsideFirstSentence_ReturnFirstBounds()
        {
            var text = "One. Two! Three?";
            // offset at 'n' (index 1) → "One. "
            var (start, end) = TextSegmentation.SentenceBounds(1, text.AsSpan());
            Assert.Equal(0, start);
            Assert.Equal(5, end); // "One. "
        }

        [Fact]
        public void OffsetInsideSecondSentence()
        {
            var text = "One. Two! Three?";
            // offset at 'T' of 'Two' (index 5) → "Two! "
            var (start, end) = TextSegmentation.SentenceBounds(5, text.AsSpan());
            Assert.Equal(5, start);
            Assert.Equal(10, end); // "Two! "
        }

        [Fact]
        public void OffsetAtEnd_ReturnsLastSegmentBounds()
        {
            var text = "Hello.";
            var (start, end) = TextSegmentation.SentenceBounds(text.Length, text.AsSpan());
            Assert.Equal(0, start);
            Assert.Equal(text.Length, end);
        }

        [Fact]
        public void Abbreviation_DoesNotBreakDecimalPoint()
        {
            // SB6: ATerm × Numeric — no break before digit
            var text = "Pi is 3.14159.";
            var (start, end) = TextSegmentation.SentenceBounds(7, text.AsSpan());
            Assert.Equal(0, start);
            Assert.Equal(text.Length, end); // whole text is one sentence
        }

        [Fact]
        public void ParagraphSeparator_BreaksImmediately()
        {
            // U+2029 PARAGRAPH SEPARATOR (Sep class) → SB4 break after it
            var text = "One.\u2029Two.";
            var (start, end) = TextSegmentation.SentenceBounds(0, text.AsSpan());
            Assert.Equal(0, start);
            // "One.\u2029" is the first sentence (break after Sep)
            Assert.Equal(5, end);
        }

        [Fact]
        public void CrLfBoundary_TreatedAsSingleBreak()
        {
            // SB3: CR × LF (no break between them); SB4 breaks after LF
            var text = "Line1\r\nLine2";
            var (start, end) = TextSegmentation.SentenceBounds(0, text.AsSpan());
            Assert.Equal(0, start);
            Assert.Equal(7, end); // "Line1\r\n"
        }

        [Fact]
        public void ATermBeforeLower_NoBreak_SB8()
        {
            // SB8: ATerm × ... Lower → "e.g. some" should not break before 's'
            var text = "e.g. some text.";
            var (start, end) = TextSegmentation.SentenceBounds(0, text.AsSpan());
            Assert.Equal(0, start);
            Assert.Equal(text.Length, end); // single sentence
        }
    }
}
