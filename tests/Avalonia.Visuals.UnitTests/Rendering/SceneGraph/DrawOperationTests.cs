﻿using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public class DrawOperationTests
    {
        [Fact]
        public void Empty_Bounds_Remain_Empty()
        {
            var target = new TestDrawOperation(Rect.Empty, Matrix.Identity, null);

            Assert.Equal(Rect.Empty, target.Bounds);
        }

        [Theory]
        [InlineData(10, 10, 10, 10, 1, 1, 1, 9, 9, 12, 12)]
        [InlineData(10, 10, 10, 10, 1, 1, 2, 9, 9, 12, 12)]
        [InlineData(10, 10, 10, 10, 1.5, 1.5, 1, 14, 14, 17, 17)]
        public void Rectangle_Bounds_Are_Snapped_To_Pixels(
            double x,
            double y,
            double width,
            double height,
            double scaleX,
            double scaleY,
            double? penThickness,
            double expectedX,
            double expectedY,
            double expectedWidth,
            double expectedHeight)
        {
            var target = new TestDrawOperation(
                new Rect(x, y, width, height),
                Matrix.CreateScale(scaleX, scaleY),
                penThickness.HasValue ? new Pen(Brushes.Black, penThickness.Value) : null);
            Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), target.Bounds);
        }

        private class TestDrawOperation : DrawOperation
        {
            public TestDrawOperation(Rect bounds, Matrix transform, Pen pen)
                :base(bounds, transform, pen)
            {
            }

            public override bool HitTest(Point p) => false;

            public override void Render(IDrawingContextImpl context) { }
        }
    }
}
