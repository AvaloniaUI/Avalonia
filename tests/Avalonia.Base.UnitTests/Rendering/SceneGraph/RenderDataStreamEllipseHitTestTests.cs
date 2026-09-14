using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Drawing;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class RenderDataStreamEllipseHitTestTests
    {
        [Theory]
        [InlineData(50, 50, true)]
        [InlineData(50, 0, true)]
        [InlineData(100, 50, true)]
        [InlineData(50, 100, true)]
        [InlineData(-1, 0, false)]
        [InlineData(101, 0, false)]
        [InlineData(101, 101, false)]
        [InlineData(0, 101, false)]
        public void FillOnly_HitTest(double x, double y, bool inside)
        {
            using var stream = new RenderDataStream();
            stream.DrawEllipse(Brushes.Black, null, null, new Rect(0, 0, 100, 100));

            Assert.Equal(inside, stream.HitTest(new Point(x, y)));
        }

        [Theory]
        [InlineData(50, 0, true)]
        [InlineData(51, 0, true)]
        [InlineData(100, 50, true)]
        [InlineData(50, 100, true)]
        [InlineData(-1, 50, true)]
        [InlineData(53, 50, false)]
        [InlineData(101, 0, false)]
        [InlineData(101, 101, false)]
        [InlineData(0, 101, false)]
        public void StrokeOnly_HitTest(double x, double y, bool inside)
        {
            var pen = new ImmutablePen(Brushes.Black, 2);

            using var stream = new RenderDataStream();
            stream.DrawEllipse(null, pen, pen, new Rect(0, 0, 100, 100));

            Assert.Equal(inside, stream.HitTest(new Point(x, y)));
        }
    }
}
