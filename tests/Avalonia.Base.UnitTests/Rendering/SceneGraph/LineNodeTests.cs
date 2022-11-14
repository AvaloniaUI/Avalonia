using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public class LineNodeTests
    {
        [Fact]
        public void HitTest_Should_Be_True()
        {
            var lineNode = new LineNode(
                Matrix.Identity,
                new Pen(Brushes.Black, 3),
                new Point(15, 10),
                new Point(150, 73));

            var pointsInside = new List<Point>()
            {
                new Point(14, 8.9),
                new Point(15, 10),
                new Point(30, 15.5),
                new Point(30, 18.5),
                new Point(150, 73),
                new Point(151, 71.9),
            };

            foreach (var point in pointsInside)
            {
                Assert.True(lineNode.HitTest(point));
            }
        }

        [Fact]
        public void HitTest_Should_Be_False()
        {
            var lineNode = new LineNode(
                Matrix.Identity,
                new Pen(Brushes.Black, 3),
                new Point(15, 10),
                new Point(150, 73));

            var pointsOutside = new List<Point>()
            {
                new Point(14, 8),
                new Point(14, 8.8),
                new Point(30, 15.3),
                new Point(30, 18.7),
                new Point(151, 71.8),
                new Point(155, 75),
            };

            foreach (var point in pointsOutside)
            {
                Assert.False(lineNode.HitTest(point));
            }
        }
    }
}
