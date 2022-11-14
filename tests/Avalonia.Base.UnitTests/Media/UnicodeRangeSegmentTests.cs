using Avalonia.Media;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class UnicodeRangeSegmentTests
    {
        [InlineData("u+00-FF", 0, 255)]
        [InlineData("U+00-FF", 0, 255)]
        [InlineData("U+00-U+FF", 0, 255)]
        [InlineData("U+AB??", 43776, 44031)]
        [Theory]
        public void Should_Parse(string s, int expectedStart, int expectedEnd)
        {
            var segment = UnicodeRangeSegment.Parse(s);

            Assert.Equal(expectedStart, segment.Start);

            Assert.Equal(expectedEnd, segment.End);
        }
    }
}
