using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing.Nodes;
using Avalonia.Rendering.SceneGraph;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public class LineNodeTests
    {
        static RenderDataLineNode LineNode(IPen pen, Point p1, Point p2) => new RenderDataLineNode
        {
            P1 = p1,
            P2 = p2,
            ServerPen = pen,
            ClientPen = pen
        };
        
        [Fact]
        public void HitTest_Should_Be_True()
        {
            var lineNode = LineNode(
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
            var lineNode = LineNode(
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
