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

        [InlineData(0)]
        [InlineData(19)]
        [InlineData(26)]
        [InlineData(100)]
        [Theory]
        public void InRange_Should_Return_False_For_Values_Outside_Range(int value)
        {
            var segment = new UnicodeRangeSegment(20, 25);

            Assert.Equal(false, segment.IsInRange(value));
        }

        [InlineData(20)]
        [InlineData(21)]
        [InlineData(22)]
        [Theory]
        public void InRange_Should_Return_True_For_Values_Within_Range(int value)
        {
            var segment = new UnicodeRangeSegment(20, 22);

            Assert.Equal(true, segment.IsInRange(value));
        }
    }
}
