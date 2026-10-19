using System;
using System.Reflection;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.UnitTests;
using Moq;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class RenderBoundsTests
    {
        [Theory,
            InlineData("M10 20 L 20 10 L 30 20", PenLineCap.Round, PenLineJoin.Miter, 2, 10, 
                9, 8.585786819458008, 22.000001907348633, 12.414215087890625),
            InlineData("M10 10 L 20 10", PenLineCap.Round, PenLineJoin.Miter,2, 10,
                9,9,12,2),
            InlineData("M10 10 L 20 15 L 10 20", PenLineCap.Flat, PenLineJoin.Miter, 2, 20,
                9.552786827087402, 9.105572700500488, 12.683281898498535, 11.788853645324707),
            InlineData("M0,0 A128,128 0 0 0 128,0", PenLineCap.Flat, PenLineJoin.Bevel, 0, 0,
                0, 0, 128, 17.14875030517578)
        ]
        public void RenderBoundsAreCorrectlyCalculated(string path, PenLineCap cap, PenLineJoin join, double thickness, double miterLimit, double x, double y, double width, double height)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                       .With(renderInterface: new PlatformRenderInterface())))
            {
                var geo = PathGeometry.Parse(path);
                var pen = new Pen(Brushes.Black, thickness, null, cap, join, miterLimit);
                var bounds = geo.GetRenderBounds(pen);
                var tolerance = 0.001;

                Assert.Equal(bounds.X, x, tolerance);
                Assert.Equal(bounds.Y, y, tolerance);
                Assert.Equal(bounds.Width, width, tolerance);
                Assert.Equal(bounds.Height, height, tolerance);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Failed_Stroke_Rebuild_Should_Preserve_Cache_And_Allow_Retry(bool hitTest)
        {
            var geometry = new RectangleGeometryImpl(new Rect(0, 0, 100, 100));
            var originalPen = new ImmutablePen(Brushes.Black, 2);
            Assert.Equal(new Rect(-1, -1, 102, 102), geometry.GetRenderBounds(originalPen));
            var originalPath = GetCachedStrokePath(geometry);

            var pen = new Mock<IPen>();
            pen.SetupGet(x => x.Thickness).Returns(4);
            pen.SetupSequence(x => x.DashStyle)
                .Returns((IDashStyle?)null)
                .Throws(new InvalidOperationException("Stroke creation failed."))
                .Returns((IDashStyle?)null);

            // The first dash-style read hashes the pen; the second rebuilds the stroke.
            Assert.Throws<InvalidOperationException>(() =>
            {
                if (hitTest)
                    geometry.StrokeContains(pen.Object, new Point(0, 50));
                else
                    geometry.GetRenderBounds(pen.Object);
            });

            // Check the old native handle before retrying: a disposed cached path can
            // otherwise crash the test process inside Skia instead of failing an assertion.
            Assert.NotEqual(IntPtr.Zero, originalPath.Handle);
            Assert.Same(originalPath, GetCachedStrokePath(geometry));

            Assert.True(geometry.StrokeContains(pen.Object, new Point(-1.5, 50)));
            Assert.Equal(new Rect(-2, -2, 104, 104), geometry.GetRenderBounds(pen.Object));
            Assert.Equal(IntPtr.Zero, originalPath.Handle);
        }

        private static SKPath GetCachedStrokePath(GeometryImpl geometry)
        {
            var cache = typeof(GeometryImpl)
                .GetField("_pathCache", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(geometry)!;
            return (SKPath)cache.GetType()
                .GetField("_path", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(cache)!;
        }
    }
}
