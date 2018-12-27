using System.Linq;

using Avalonia.Media;
using Avalonia.Skia.Text;

using SkiaSharp;

using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class SKTextLayoutTests
    {
        private static readonly string SingleLineText = "0123456789";
        private static readonly string MultiLineText = "123456789\r123456789\r123456789\r123456789\r";

        [Fact]
        public void ShouldApplyTextStyleSpanToTextInBetween()
        {
            const string Text = "012345";

            var layout = new SKTextLayout(
                Text,
                SKTypeface.FromFamilyName(null),
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(1, 2, effectBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(3, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("12", textRun.Text);

            Assert.Equal(effectBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextAtStart()
        {
            var layout = new SKTextLayout(
                SingleLineText,
                SKTypeface.FromFamilyName(null),
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(0, 2, effectBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(2, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[0];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("01", textRun.Text);

            Assert.Equal(effectBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextAtEnd()
        {
            var layout = new SKTextLayout(
                SingleLineText,
                SKTypeface.FromFamilyName(null),
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(8, 2, effectBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(2, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("89", textRun.Text);

            Assert.Equal(effectBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToSingleCharacter()
        {
            var layout = new SKTextLayout(
                "0",
                SKTypeface.FromFamilyName(null),
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(0, 1, effectBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(1, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[0];

            Assert.Equal(1, textRun.Text.Length);

            Assert.Equal("0", textRun.Text);

            Assert.Equal(effectBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextSpanToUnicodeStringInBetween()
        {
            const string Text = "😀 😀 😀 😀";

            var layout = new SKTextLayout(
                Text,
                SKTypeface.FromFamilyName(null),
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(4, 1, effectBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(3, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("😀", textRun.Text);

            Assert.Equal(effectBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void TextLengthShouldBeEqualToTextLineLengthSum()
        {
            var layout = new SKTextLayout(
                MultiLineText,
                SKTypeface.FromFamilyName(null),
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(MultiLineText.Length, layout.TextLines.Sum(x => x.Length));
        }

        [Fact]
        public void TextLengthShouldBeEqualToTextRunTextLengthSum()
        {
            var layout = new SKTextLayout(
                MultiLineText,
                SKTypeface.FromFamilyName(null),
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(
                MultiLineText.Length,
                layout.TextLines.Select(textLine => textLine.TextRuns.Sum(textRun => textRun.Text.Length)).Sum());
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToMultiLine()
        {
            var layout = new SKTextLayout(
                MultiLineText,
                SKTypeface.FromFamilyName(null),
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(200, 125));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(5, 20, effectBrush));

            Assert.Equal(effectBrush, layout.TextLines[0].TextRuns[1].DrawingEffect);
            Assert.Equal(effectBrush, layout.TextLines[1].TextRuns[0].DrawingEffect);
            Assert.Equal(effectBrush, layout.TextLines[2].TextRuns[0].DrawingEffect);
        }

        [Fact]
        public void ShouldHitTestSurrogatePair()
        {
            const string Text = "😀";

            var layout = new SKTextLayout(
                Text,
                SKTypeface.FromFamilyName(null),
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var lineMetrics = layout.TextLines[0].LineMetrics;

            var width = lineMetrics.Size.Width;

            var hitTestResult = layout.HitTestPoint(new Point(width, lineMetrics.BaselineOrigin.Y));

            Assert.Equal(0, hitTestResult.TextPosition);

            Assert.Equal(2, hitTestResult.Length);
        }
    }
}
