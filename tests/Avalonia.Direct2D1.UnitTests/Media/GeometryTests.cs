using Avalonia.Media;
using Xunit;

namespace Avalonia.Direct2D1.UnitTests.Media
{
    public class GeometryTests
    {
        private static readonly RectComparer Compare = new RectComparer();

        [Fact]
        public void Should_Measure_Expander_Triangle_Correctly()
        {
            using (AvaloniaLocator.EnterScope())
            {
                Direct2D1Platform.Initialize();

                var target = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z");

                Assert.Equal(new Rect(0, 2, 4, 8), target.Bounds, Compare);
            }
        }

        [Fact]
        public void Should_Measure_Expander_Triangle_With_Stroke_Correctly()
        {
            using (AvaloniaLocator.EnterScope())
            {
                Direct2D1Platform.Initialize();

                var target = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z");
                var pen = new Pen(Brushes.Black, 2);

                Assert.Equal(new Rect(-1, -0.414, 6.414, 12.828), target.GetRenderBounds(pen), Compare);
            }
        }
    }
}
