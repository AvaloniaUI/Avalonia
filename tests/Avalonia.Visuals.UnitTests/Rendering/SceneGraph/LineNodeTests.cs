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
                new Point(15, 15),
                new Point(150, 150));


            List<Point> pointsInside = new()
            {
                new Point(14, 14),
                new Point(15, 15),
                new Point(32.1, 30),
                new Point(30, 32.1),
                new Point(150, 150),
                new Point(151, 151),
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
                new Point(15, 15),
                new Point(150, 150));


            List<Point> pointsOutside= new()
            {
                new Point(13.9, 13.9),
                new Point(30, 32.2),
                new Point(32.2, 30),
                new Point(151.1, 151.1),
                new Point(200, 200),
            };

            foreach (var point in pointsOutside)
            {
                Assert.False(lineNode.HitTest(point)); 
            }
        }
    }
}
