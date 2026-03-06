using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class BoxShadowsTests
    {
        [Theory]
        [InlineData("none")]
        [InlineData(" none ")]
        public void Parse_None_ReturnsEmpty(string input)
        {
            var bs = BoxShadows.Parse(input);
            Assert.Equal(0, bs.Count);
            Assert.Equal(default, bs);
            Assert.Equal("none", bs.ToString());
        }

        [Theory]
        [InlineData("0 0 5 0 #FF0000")]
        [InlineData("10 20 30 5 rgba(0,0,0,0.5)")]
        [InlineData("10 20 30 5 rgba(0, 0, 0, 0.5)")]
        [InlineData("  10  20  30  5  rgba(0,  0,  0,  0.5)  ")]
        public void Parse_SingleShadow_ToString_RoundTrip(string input)
        {
            var bs = BoxShadows.Parse(input);
            Assert.Equal(1, bs.Count);
            var str = bs.ToString();
            var reparsed = BoxShadows.Parse(str);
            Assert.Equal(bs, reparsed);
        }

        [Theory]
        [InlineData("0 0 5 0 #FF0000", 10.0)]
        [InlineData("0 0 10 0 rgba(0,0,0,0.5)", 20.0)]
        public void TransformBounds_IncludesShadowExpansion(string input, double minExpansion)
        {
            var bs = BoxShadows.Parse(input);
            var rect = new Rect(0, 0, 100, 100);
            var transformed = bs.TransformBounds(rect);
            Assert.True(transformed.Width >= rect.Width + minExpansion);
            Assert.True(transformed.Height >= rect.Height + minExpansion);
        }

        [Theory]
        [InlineData("5 5 10 0 rgba(10,20,30,0.4)")]
        [InlineData("5 5 10 0 hsla(10,20%,30%,0.4)")]
        [InlineData("5 5 10 0 hsva(10,20%,30%,0.4)")]
        public void Parse_ColorFunction_IsHandled(string input)
        {
            var bs = BoxShadows.Parse(input);
            Assert.Equal(1, bs.Count);
            var reparsed = BoxShadows.Parse(bs.ToString());
            Assert.Equal(bs, reparsed);
        }

        [Theory]
        [InlineData("1 2 3 0 #FF0000", 1)]
        [InlineData("10 20 30 5 rgba(0,0,0,0.5)", 1)]
        [InlineData("1 2 3 0 #FF0000, 1 2 3 0 #FF0000", 2)]
        [InlineData("10 20 30 5 rgba(0,0,0,0.5), 1 2 3 0 #FF0000", 2)]
        [InlineData("10 20 30 5 rgba(0,0,0,0.5), 10 20 30 5 rgba(0,0,0,0.5)", 2)]
        [InlineData("10 20 30 5 rgba(0,0,0,0.5), 10 20 30 5 rgba(0,0,0,0.5), 10 20 30 5 rgba(0,0,0,0.5)", 3)]
        [InlineData("10 20 30 5 rgba(0,0,0,0.5), 10 20 30 5 #ffffff, 10 20 30 5 Red", 3)]
        [InlineData("  10 20 30 5 rgba(0, 0, 0, 0.5), 10 20 30 5 rgba(0, 0, 0, 0.5), 10 20 30 5 rgba(0, 0, 0, 0.5)  ", 3)]
        [InlineData("  10 20 30 5 rgba(0, 0, 0, 0.5), 10 20 30 5 #ffffff, 10 20 30 5 Red  ", 3)]
        public void Parse_MultipleShadows(string input, int count)
        {
            var bs = BoxShadows.Parse(input);
            Assert.Equal(count, bs.Count);
            var reparsed = BoxShadows.Parse(bs.ToString());
            Assert.Equal(bs, reparsed);
        }
    }
}
