using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class RenderDataStreamLineHitTestTests
    {
        private static RenderDataStream LineStream(IPen pen, Point p1, Point p2)
        {
            var stream = new RenderDataStream();
            stream.DrawLine(pen, pen, p1, p2);
            return stream;
        }

        [Fact]
        public void HitTest_Should_Be_True()
        {
            using var stream = LineStream(new Pen(Brushes.Black, 3), new Point(15, 10), new Point(150, 73));

            var pointsInside = new List<Point>
            {
                new Point(14, 8.9),
                new Point(15, 10),
                new Point(30, 15.5),
                new Point(30, 18.5),
                new Point(150, 73),
                new Point(151, 71.9),
            };

            foreach (var point in pointsInside)
                Assert.True(stream.HitTest(point));
        }

        [Fact]
        public void HitTest_Should_Be_False()
        {
            using var stream = LineStream(new Pen(Brushes.Black, 3), new Point(15, 10), new Point(150, 73));

            var pointsOutside = new List<Point>
            {
                new Point(14, 8),
                new Point(14, 8.8),
                new Point(30, 15.3),
                new Point(30, 18.7),
                new Point(151, 71.8),
                new Point(155, 75),
            };

            foreach (var point in pointsOutside)
                Assert.False(stream.HitTest(point));
        }
    }
}
