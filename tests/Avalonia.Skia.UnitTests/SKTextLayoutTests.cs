namespace Avalonia.Skia.UnitTests
{
    using System.Linq;

    using Avalonia.Media;

    using SkiaSharp;

    using Xunit;

    public class SKTextLayoutTests
    {
        [Fact]
        public void ShouldApplyTextStyleSpanToTextInBetween()
        {
            const string Text = "012345";

            var layout = new SKTextLayout(Text, SKTypeface.FromFamilyName(null), 12.0f, TextAlignment.Left, TextWrapping.NoWrap, new Size(double.PositiveInfinity, double.PositiveInfinity));

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
            const string Text = "012345";

            var layout = new SKTextLayout(Text, SKTypeface.FromFamilyName(null), 12.0f, TextAlignment.Left, TextWrapping.NoWrap, new Size(double.PositiveInfinity, double.PositiveInfinity));

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
            const string Text = "012345";

            var layout = new SKTextLayout(Text, SKTypeface.FromFamilyName(null), 12.0f, TextAlignment.Left, TextWrapping.NoWrap, new Size(double.PositiveInfinity, double.PositiveInfinity));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(4, 2, effectBrush));

            var textLine = layout.TextLines[0];

            Assert.Equal(2, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.Text.Length);

            Assert.Equal("45", textRun.Text);

            Assert.Equal(effectBrush, textRun.DrawingEffect);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToSingleCharacter()
        {
            const string Text = "0";

            var layout = new SKTextLayout(Text, SKTypeface.FromFamilyName(null), 12.0f, TextAlignment.Left, TextWrapping.NoWrap, new Size(double.PositiveInfinity, double.PositiveInfinity));

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
        public void TextLengthShouldBeEqualToTextLineLengthSum()
        {
            const string Text = "Multiline TextBox with TextWrapping.&#xD;&#xD;Lorem ipsum dolor sit amet, consectetur adipiscing elit.";

            var layout = new SKTextLayout(Text, SKTypeface.FromFamilyName(null), 12.0f, TextAlignment.Left, TextWrapping.NoWrap, new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(Text.Length, layout.TextLines.Sum(x => x.Length));
        }

        [Fact]
        public void TextLengthShouldBeEqualToTextRunTextLengthSum()
        {
            const string Text = "Multiline TextBox with TextWrapping.&#xD;&#xD;Lorem ipsum dolor sit amet, consectetur adipiscing elit.";

            var layout = new SKTextLayout(Text, SKTypeface.FromFamilyName(null), 12.0f, TextAlignment.Left, TextWrapping.NoWrap, new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(
                Text.Length,
                layout.TextLines.Select(textLine => textLine.TextRuns.Sum(textRun => textRun.Text.Length)).Sum());
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToMultiLine()
        {
            const string Text = "Multiline TextBox with TextWrapping.\r\rLorem ipsum dolor sit amet, consectetur adipiscing elit.";

            var layout = new SKTextLayout(Text, SKTypeface.FromFamilyName(null), 12.0f, TextAlignment.Left, TextWrapping.Wrap, new Size(200, 125));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(0, Text.Length, effectBrush));

            foreach (var textLine in layout.TextLines)
            {
                foreach (var textRun in textLine.TextRuns)
                {
                    Assert.Equal(effectBrush, textRun.DrawingEffect);
                }
            }           
        }
    }
}
