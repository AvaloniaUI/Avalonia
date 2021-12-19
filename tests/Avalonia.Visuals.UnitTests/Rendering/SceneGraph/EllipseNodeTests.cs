﻿using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.SceneGraph;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public class EllipseNodeTests
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
            var ellipseNode = new EllipseNode(Matrix.Identity, Brushes.Black, null, new Rect(0,0, 100, 100), null);

            var point = new Point(x, y);

            Assert.True(ellipseNode.HitTest(point) == inside);
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
            var ellipseNode = new EllipseNode(Matrix.Identity, null, new ImmutablePen(Brushes.Black, 2), new Rect(0, 0, 100, 100), null);

            var point = new Point(x, y);

            Assert.Equal(inside, ellipseNode.HitTest(point));
        }
    }
}
