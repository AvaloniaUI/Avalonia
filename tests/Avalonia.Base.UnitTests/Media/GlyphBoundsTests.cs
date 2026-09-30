using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GlyphBoundsTests
    {
        [Fact]
        public void Width_And_Height_Are_Extents_For_A_Well_Formed_Header()
        {
            var bounds = new GlyphBounds(XMin: 10, YMin: 20, XMax: 110, YMax: 220);

            Assert.Equal(100, bounds.Width);
            Assert.Equal(200, bounds.Height);
        }

        [Fact]
        public void Width_And_Height_Clamp_To_Zero_When_Max_Is_Below_Min()
        {
            // A malformed glyf header (xMax < xMin / yMax < yMin) must not produce a
            // negative extent that wraps to a huge value when narrowed to a ushort.
            var bounds = new GlyphBounds(XMin: 100, YMin: 100, XMax: 50, YMax: 40);

            Assert.Equal(0, bounds.Width);
            Assert.Equal(0, bounds.Height);

            // The clamp also keeps the value inside ushort range.
            Assert.True(bounds.Width <= ushort.MaxValue);
            Assert.True(bounds.Height <= ushort.MaxValue);
        }

        [Fact]
        public void Maximum_Extent_For_Int16_Coordinates_Fits_In_Ushort()
        {
            // short.MinValue..short.MaxValue gives the widest possible extent, 65535,
            // which is exactly ushort.MaxValue — so narrowing the clamped value never overflows.
            var bounds = new GlyphBounds(short.MinValue, short.MinValue, short.MaxValue, short.MaxValue);

            Assert.Equal(ushort.MaxValue, bounds.Width);
            Assert.Equal(ushort.MaxValue, bounds.Height);
        }
    }
}
