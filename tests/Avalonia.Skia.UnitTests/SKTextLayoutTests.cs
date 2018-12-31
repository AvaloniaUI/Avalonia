using System.Linq;

using Avalonia.Media;
using Avalonia.Skia.Text;

using SkiaSharp;

using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class SKTextLayoutTests
    {
        private static readonly string s_singleLineText = "0123456789";
        private static readonly string s_multiLineText = "123456789\r123456789\r123456789\r123456789\r";
        private static readonly SKTypeface s_typeface;

        static SKTextLayoutTests()
        {
            using (var stream =
                typeof(SKTextLayoutTests).Assembly.GetManifestResourceStream(
                    "Avalonia.Skia.UnitTests.Assets.NotoEmoji-Regular.ttf"))
            {
                s_typeface = SKTypeface.FromStream(stream);
            }             
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextInBetween()
        {
            const string text = "012345";

            var layout = new SKTextLayout(
                text,
                s_typeface,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var foregroundBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(1, 2, foregroundBrush: foregroundBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(3, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("12", textRun.Text);

            Assert.Equal(foregroundBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextAtStart()
        {
            var layout = new SKTextLayout(
                s_singleLineText,
                s_typeface,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var foregroundBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(0, 2, foregroundBrush: foregroundBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(2, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[0];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("01", textRun.Text);

            Assert.Equal(foregroundBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextAtEnd()
        {
            var layout = new SKTextLayout(
                s_singleLineText,
                s_typeface,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var foregroundBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(8, 2, foregroundBrush: foregroundBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(2, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("89", textRun.Text);

            Assert.Equal(foregroundBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToSingleCharacter()
        {
            var layout = new SKTextLayout(
                "0",
                s_typeface,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(0, 1, foregroundBrush: effectBrush));

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
            const string text = "😀 😀 😀 😀";

            var layout = new SKTextLayout(
                text,
                s_typeface,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var foregroundBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(4, 1, foregroundBrush: foregroundBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(3, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("😀", textRun.Text);

            Assert.Equal(foregroundBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void TextLengthShouldBeEqualToTextLineLengthSum()
        {
            var layout = new SKTextLayout(
                s_multiLineText,
                s_typeface,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(s_multiLineText.Length, layout.TextLines.Sum(x => x.Length));
        }

        [Fact]
        public void TextLengthShouldBeEqualToTextRunTextLengthSum()
        {
            var layout = new SKTextLayout(
                s_multiLineText,
                s_typeface,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(
                s_multiLineText.Length,
                layout.TextLines.Select(textLine => textLine.TextRuns.Sum(textRun => textRun.Text.Length)).Sum());
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToMultiLine()
        {
            var layout = new SKTextLayout(
                s_multiLineText,
                s_typeface,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(200, 125));

            var foregroundBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(5, 20, foregroundBrush: foregroundBrush));

            Assert.Equal(foregroundBrush, layout.TextLines[0].TextRuns[1].DrawingEffect);
            Assert.Equal(foregroundBrush, layout.TextLines[1].TextRuns[0].DrawingEffect);
            Assert.Equal(foregroundBrush, layout.TextLines[2].TextRuns[0].DrawingEffect);
        }

        [Fact]
        public void ShouldHitTestSurrogatePair()
        {
            const string text = "😀";

            var layout = new SKTextLayout(
                text,
                s_typeface,
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
