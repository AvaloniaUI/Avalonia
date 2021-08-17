using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Shapes
{
    public class PathTests
    {
        [Fact]
        public void Path_With_Null_Data_Does_Not_Throw_On_Measure()
        {
            var target = new Path();

            target.Measure(Size.Infinity);
        }

        [Fact]
        public void Subscribes_To_Geometry_Changes()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var target = new Path { Data = geometry };

            var root = new TestRoot(target);

            target.Measure(Size.Infinity);
            Assert.True(target.IsMeasureValid);

            geometry.Rect = new Rect(0, 0, 20, 20);

            Assert.False(target.IsMeasureValid);

            root.Child = null;
        }

        [Theory]
        [InlineData(Stretch.None, 100, 200)]
        [InlineData(Stretch.Fill, 500, 500)]
        [InlineData(Stretch.Uniform, 250, 500)]
        [InlineData(Stretch.UniformToFill, 500, 500)]
        public void Calculates_Correct_DesiredSize_For_Finite_Bounds(Stretch stretch, double expectedWidth, double expectedHeight)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Path()
            {
                Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
                Stretch = stretch,
            };

            target.Measure(new Size(500, 500));

            Assert.Equal(new Size(expectedWidth, expectedHeight), target.DesiredSize);
        }

        [Theory]
        [InlineData(Stretch.None)]
        [InlineData(Stretch.Fill)]
        [InlineData(Stretch.Uniform)]
        [InlineData(Stretch.UniformToFill)]
        public void Calculates_Correct_DesiredSize_For_Infinite_Bounds(Stretch stretch)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Path()
            {
                Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
                Stretch = stretch,
            };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(new Size(100, 200), target.DesiredSize);
        }

        [Fact]
        public void Measure_Does_Not_Update_RenderedGeometry_Transform()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Path
            {
                Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
                Stretch = Stretch.Fill,
            };

            target.Measure(new Size(500, 500));

            Assert.Null(target.RenderedGeometry.Transform);
        }

        [Theory]
        [InlineData(Stretch.None, 1, 1)]
        [InlineData(Stretch.Fill, 5, 2.5)]
        [InlineData(Stretch.Uniform, 2.5, 2.5)]
        [InlineData(Stretch.UniformToFill, 5, 5)]
        public void Arrange_Updates_RenderedGeometry_Transform(Stretch stretch, double expectedScaleX, double expectedScaleY)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Path
            {
                Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
                Stretch = stretch,
            };

            target.Measure(new Size(500, 500));
            target.Arrange(new Rect(0, 0, 500, 500));

            if (expectedScaleX == 1 && expectedScaleY == 1)
            {
                Assert.Null(target.RenderedGeometry.Transform);
            }
            else
            {
                Assert.Equal(Matrix.CreateScale(expectedScaleX, expectedScaleY), target.RenderedGeometry.Transform.Value);
            }
        }

        [Fact]
        public void Arrange_Reserves_All_Of_Arrange_Rect()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            RectangleGeometry geometry;
            var target = new Path
            {
                Data = geometry = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
                Stretch = Stretch.Uniform,
            };

            target.Measure(new Size(400, 400));
            target.Arrange(new Rect(0, 0, 400, 400));

            Assert.Equal(new Rect(0, 0, 100, 200), geometry.Rect);
            Assert.Equal(Matrix.CreateScale(2, 2), target.RenderedGeometry.Transform.Value);
            Assert.Equal(new Rect(0, 0, 400, 400), target.Bounds);
        }

        [Fact]
        public void Measure_Without_Arrange_Does_Not_Clear_RenderedGeometry_Transform()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Path
            {
                Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 100) },
                Stretch = Stretch.Fill,
            };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(0, 0, 200, 200));

            Assert.Equal(Matrix.CreateScale(2, 2), target.RenderedGeometry.Transform.Value);

            target.Measure(new Size(300, 300));

            Assert.Equal(Matrix.CreateScale(2, 2), target.RenderedGeometry.Transform.Value);
        }

        [Fact]
        public void Arrange_Without_Measure_Updates_RenderedGeometry_Transform()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Path 
            { 
                Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 100) },
                Stretch = Stretch.Fill,
            };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(0, 0, 200, 200));
            Assert.Equal(Matrix.CreateScale(2, 2), target.RenderedGeometry.Transform.Value);

            target.Arrange(new Rect(0, 0, 300, 300));
            Assert.Equal(Matrix.CreateScale(3, 3), target.RenderedGeometry.Transform.Value);
        }
    }
}
