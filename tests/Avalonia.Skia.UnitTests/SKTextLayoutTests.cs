namespace Avalonia.Skia.UnitTests
{
    using System.Linq;

    using Avalonia.Media;

    using SkiaSharp;

    using Xunit;

    public class SKTextLayoutTests
    {
        [Fact]
        public void ShouldApplyTextStyleSpanToTextRun()
        {
            var text = "012345\r\r012345";

            var layout = new SKTextLayout(text, SKTypeface.FromFamilyName(null), 12.0f, TextAlignment.Left, TextWrapping.NoWrap, new Size(double.PositiveInfinity, double.PositiveInfinity));

            var effectBrush = new SolidColorBrush(Colors.Red);

            layout.ApplyTextSpan(new FormattedTextStyleSpan(4, 4, effectBrush));

            Assert.Equal(layout.TextLines.Count, 3);

            var textLine = layout.TextLines.First();

            Assert.Equal(textLine.TextRuns.Count, 2);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(textRun.Text.Length, 2);

            Assert.Equal(textRun.DrawingEffect, effectBrush);
        }
    }
}
