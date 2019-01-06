using System.Collections.Generic;
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
        private static readonly SKTypeface s_emojiTypeface;

        static SKTextLayoutTests()
        {
            using (var stream =
                typeof(SKTextLayoutTests).Assembly.GetManifestResourceStream(
                    "Avalonia.Skia.UnitTests.Assets.NotoEmoji-Regular.ttf"))
            {
                s_emojiTypeface = SKTypeface.FromStream(stream);
            }
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextInBetween()
        {
            const string text = "012345";

            var drawingEffect = new SolidColorBrush(Colors.Red);

            var spans = new List<FormattedTextStyleSpan>
                {new FormattedTextStyleSpan(1, 2, drawingEffect: drawingEffect)};

            var layout = new SKTextLayout(
                text,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(3, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("12", textRun.Text);

            Assert.Equal(drawingEffect, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextAtStart()
        {
            var drawingEffect = new SolidColorBrush(Colors.Red);

            var spans = new List<FormattedTextStyleSpan>
                        {
                            new FormattedTextStyleSpan(0, 2, drawingEffect: drawingEffect)
                        };

            var layout = new SKTextLayout(
                s_singleLineText,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(2, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[0];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("01", textRun.Text);

            Assert.Equal(drawingEffect, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextAtEnd()
        {
            var drawingEffect = new SolidColorBrush(Colors.Red);

            var spans = new List<FormattedTextStyleSpan>
                {new FormattedTextStyleSpan(8, 2, drawingEffect: drawingEffect)};

            var layout = new SKTextLayout(
                s_singleLineText,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(2, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("89", textRun.Text);

            Assert.Equal(drawingEffect, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToSingleCharacter()
        {
            var drawingEffect = new SolidColorBrush(Colors.Red);

            var spans = new List<FormattedTextStyleSpan>
                {new FormattedTextStyleSpan(0, 1, drawingEffect: drawingEffect)};

            var layout = new SKTextLayout(
                "0",
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(1, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[0];

            Assert.Equal(1, textRun.Text.Length);

            Assert.Equal("0", textRun.Text);

            Assert.Equal(drawingEffect, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextSpanToUnicodeStringInBetween()
        {
            const string text = "😀 😀 😀 😀";

            var drawingEffect = new SolidColorBrush(Colors.Red);

            var spans = new List<FormattedTextStyleSpan>
                {new FormattedTextStyleSpan(4, 1, drawingEffect: drawingEffect)};

            var layout = new SKTextLayout(
                text,
                s_emojiTypeface,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(3, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("😀", textRun.Text);

            Assert.Equal(drawingEffect, textRun.DrawingEffect);
        }

        [Fact]
        public void TextLengthShouldBeEqualToTextLineLengthSum()
        {
            var layout = new SKTextLayout(
                s_multiLineText,
                SKTypeface.Default, 
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
                SKTypeface.Default,
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
            var drawingEffect = new SolidColorBrush(Colors.Red);

            var spans = new List<FormattedTextStyleSpan>
                {new FormattedTextStyleSpan(5, 20, drawingEffect: drawingEffect)};

            var layout = new SKTextLayout(
                s_multiLineText,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                new Size(200, 125),
                spans);

            Assert.Equal(drawingEffect, layout.TextLines[0].TextRuns[1].DrawingEffect);
            Assert.Equal(drawingEffect, layout.TextLines[1].TextRuns[0].DrawingEffect);
            Assert.Equal(drawingEffect, layout.TextLines[2].TextRuns[0].DrawingEffect);
        }

        [Fact]
        public void ShouldHitTestSurrogatePair()
        {
            const string text = "😀";

            var layout = new SKTextLayout(
                text,
                s_emojiTypeface,
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
