using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Xunit;


#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Shapes
#endif
{
    public class ShapeTests : TestBase
    {
        public ShapeTests()
            : base(@"Shapes\Shape")
        { }

        [Fact]
        public void Shape_Transformation_Calculation_Should_Be_Deferred_To_Arrange_When_Strech_Is_Fill_And_Aviable_Size_Is_Infinite()
        {
            var shape = new Polygon()
            {
                Points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(10, 5),
                    new Point(0, 10)
                },
                Stretch = Stretch.Fill
            };

            var availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            shape.Measure(availableSize);
            Geometry postMeasureGeometry = shape.RenderedGeometry;
            Transform postMeasureTransform = postMeasureGeometry.Transform;

            var finalSize = new Size(100, 50);
            var finalRect = new Rect(finalSize);
            shape.Arrange(finalRect);

            Geometry postArrangeGeometry = shape.RenderedGeometry;
            Transform postArrangeTransform = postArrangeGeometry.Transform;

            Assert.NotEqual(postMeasureGeometry, postArrangeGeometry);
            Assert.NotEqual(postMeasureTransform, postArrangeTransform);
            Assert.Equal(finalSize, shape.Bounds.Size);
        }

        [Fact]
        public void Shape_Transformation_Calculation_Should_Not_Be_Deferred_To_Arrange_When_Strech_Is_Fill_And_Aviable_Size_Is_Finite()
        {
            var shape = new Polygon()
            {
                Points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(10, 5),
                    new Point(0, 10)
                },
                Stretch = Stretch.Fill
            };

            var availableSize = new Size(100, 50);
            shape.Measure(availableSize);
            Geometry postMeasureGeometry = shape.RenderedGeometry;
            Transform postMeasureTransform = postMeasureGeometry.Transform;

            var finalRect = new Rect(availableSize);
            shape.Arrange(finalRect);

            Geometry postArrangeGeometry = shape.RenderedGeometry;
            Transform postArrangeTransform = postArrangeGeometry.Transform;

            Assert.Equal(postMeasureGeometry, postArrangeGeometry);
            Assert.Equal(postMeasureTransform, postArrangeTransform);
            Assert.Equal(availableSize, shape.Bounds.Size);
        }

        [Fact]
        public void Shape_Transformation_Calculation_Should_Not_Be_Deferred_To_Arrange_When_Strech_Is_None()
        {
            var shape = new Polygon()
            {
                Points = new List<Point>
                {
                    new Point(0, 0),
                    new Point(10, 5),
                    new Point(0, 10)
                },
                Stretch = Stretch.None
            };

            var availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            shape.Measure(availableSize);
            Geometry postMeasureGeometry = shape.RenderedGeometry;
            Transform postMeasureTransform = postMeasureGeometry.Transform;

            var finalSize = new Size(100, 50);
            var finalRect = new Rect(finalSize);
            shape.Arrange(finalRect);

            Geometry postArrangeGeometry = shape.RenderedGeometry;
            Transform postArrangeTransform = postArrangeGeometry.Transform;

            Assert.Equal(postMeasureGeometry, postArrangeGeometry);
            Assert.Equal(postMeasureTransform, postArrangeTransform);
            Assert.Equal(finalSize, shape.Bounds.Size);
        }
    }
}
