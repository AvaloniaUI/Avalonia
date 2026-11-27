using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextLayoutRangeContractTests
    {
        [Theory]
        [InlineData("abc", 0)]
        [InlineData("abc", 1)]
        [InlineData("abc", 3)]
        [InlineData("a\r\nb\r\n", 3)]
        public void Empty_Range_Returns_No_Geometry(string text, int start)
        {
            using var app = Start();
            using var layout = new TextLayout(text, Typeface.Default, 12, Brushes.Black);

            Assert.Empty(layout.HitTestTextRange(start, 0));
        }

        [Fact]
        public void Completed_Range_Does_Not_Add_Geometry_From_Following_Lines()
        {
            using var app = Start();
            using var layout = new TextLayout("a\r\nb\r\n", Typeface.Default, 12, Brushes.Black);
            var firstLine = layout.TextLines[0];
            var expected = Assert.Single(firstLine.GetTextBounds(0, firstLine.Length)).Rectangle;

            var actual = Assert.Single(layout.HitTestTextRange(0, firstLine.Length));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Negative_Range_Length_Preserves_Line_Geometry()
        {
            using var app = Start();
            using var layout = new TextLayout("abc", Typeface.Default, 12, Brushes.Black);
            var expected = layout.TextLines[0].GetTextBounds(2, -1)
                .Select(bounds => bounds.Rectangle).ToArray();
            Assert.Empty(expected);

            Assert.Equal(expected, layout.HitTestTextRange(2, -1).ToArray());
        }

        private static IDisposable Start() =>
            UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(
                renderInterface: new PlatformRenderInterface(null),
                fontManagerImpl: new CustomFontManagerImpl()));
    }
}
