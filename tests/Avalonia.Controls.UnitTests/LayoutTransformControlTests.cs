using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class LayoutTransformControlTests
    {
        [Fact]
        public void Measure_On_Scale_x2_Is_Correct()
        {
            double scale = 2;

            TransformMeasureSizeTest(
                new Size(100, 50),
                new ScaleTransform() { ScaleX = scale, ScaleY = scale },
                new Size(200, 100));
        }

        [Fact]
        public void Measure_On_Scale_x0_5_Is_Correct()
        {
            double scale = 0.5;

            TransformMeasureSizeTest(
                new Size(100, 50),
                new ScaleTransform() { ScaleX = scale, ScaleY = scale },
                new Size(50, 25));
        }

        [Fact]
        public void Measure_On_Skew_X_axis_45_degrees_Is_Correct()
        {
            TransformMeasureSizeTest(
                new Size(100, 100),
                new SkewTransform() { AngleX = 45 },
                new Size(200, 100));

        }

        [Fact]
        public void Measure_On_Skew_Y_axis_45_degrees_Is_Correct()
        {
            TransformMeasureSizeTest(
                new Size(100, 100),
                new SkewTransform() { AngleY = 45 },
                new Size(100, 200));
        }

        [Fact]
        public void Measure_On_Skew_X_axis_minus_45_degrees_Is_Correct()
        {
            TransformMeasureSizeTest(
                new Size(100, 100),
                new SkewTransform() { AngleX = -45 },
                new Size(200, 100));
        }

        [Fact]
        public void Measure_On_Skew_Y_axis_minus_45_degrees_Is_Correct()
        {
            TransformMeasureSizeTest(
                new Size(100, 100),
                new SkewTransform() { AngleY = -45 },
                new Size(100, 200));
        }

        [Fact]
        public void Measure_On_Skew_0_degrees_Is_Correct()
        {
            TransformMeasureSizeTest(
                new Size(100, 100),
                new SkewTransform() { AngleX = 0, AngleY = 0 },
                new Size(100, 100));
        }

        [Fact]
        public void Measure_On_Rotate_90_degrees_Is_Correct()
        {
            TransformMeasureSizeTest(
                new Size(100, 25),
                new RotateTransform() { Angle = 90 },
                new Size(25, 100));
        }

        [Fact]
        public void Measure_On_Rotate_minus_90_degrees_Is_Correct()
        {
            TransformMeasureSizeTest(
                new Size(100, 25),
                new RotateTransform() { Angle = -90 },
                new Size(25, 100));
        }

        [Fact]
        public void Measure_On_Rotate_0_degrees_Is_Correct()
        {
            TransformMeasureSizeTest(
                new Size(100, 25),
                new RotateTransform() { Angle = 0 },
                new Size(100, 25));
        }

        [Fact]
        public void Measure_On_Rotate_180_degrees_Is_Correct()
        {
            TransformMeasureSizeTest(
                new Size(100, 25),
                new RotateTransform() { Angle = 180 },
                new Size(100, 25));
        }

        [Fact]
        public void Bounds_On_Scale_x2_Are_correct()
        {
            double scale = 2;

            TransformRootBoundsTest(
                new Size(100, 50),
                new ScaleTransform() { ScaleX = scale, ScaleY = scale },
                new Rect(0, 0, 100, 50));
        }

        [Fact]
        public void Bounds_On_Scale_x0_5_Are_correct()
        {
            double scale = 0.5;

            TransformRootBoundsTest(
                new Size(100, 50),
                new ScaleTransform() { ScaleX = scale, ScaleY = scale },
                new Rect(0, 0, 100, 50));
        }

        [Fact]
        public void Bounds_On_Rotate_180_degrees_Are_correct()
        {
            TransformRootBoundsTest(
                new Size(100, 25),
                new RotateTransform() { Angle = 180 },
                new Rect(100, 25, 100, 25));
        }

        [Fact]
        public void Bounds_On_Rotate_0_degrees_Are_correct()
        {
            TransformRootBoundsTest(
                new Size(100, 25),
                new RotateTransform() { Angle = 0 },
                new Rect(0, 0, 100, 25));
        }

        [Fact]
        public void Bounds_On_Rotate_90_degrees_Are_correct()
        {
            TransformRootBoundsTest(
                new Size(100, 25),
                new RotateTransform() { Angle = 90 },
                new Rect(25, 0, 100, 25));
        }

        [Fact]
        public void Bounds_On_Rotate_minus_90_degrees_Are_correct()
        {
            TransformRootBoundsTest(
                new Size(100, 25),
                new RotateTransform() { Angle = -90 },
                new Rect(0, 100, 100, 25));
        }

        [Fact]
        public void Bounds_On_Transform_Applied_Then_Removed_Are_Correct()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var control = CreateWithChildAndMeasureAndTransform(
                100,
                25,
                new RotateTransform { Angle = 90 });

            Assert.Equal(new Size(25, 100), control.DesiredSize);

            control.LayoutTransform = null;
            control.Measure(Size.Infinity);
            control.Arrange(new Rect(control.DesiredSize));

            Assert.Equal(new Size(100, 25), control.DesiredSize);
        }

        [Fact]
        public void Should_Generate_RotateTransform_90_degrees()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            LayoutTransformControl lt = CreateWithChildAndMeasureAndTransform(
                                        100,
                                        25,
                                        new RotateTransform() { Angle = 90 });

            Assert.NotNull(lt.TransformRoot.RenderTransform);

            Matrix m = lt.TransformRoot.RenderTransform.Value;

            Matrix res = Matrix.CreateRotation(Matrix.ToRadians(90));

            Assert.Equal(m.M11, res.M11, 3);
            Assert.Equal(m.M12, res.M12, 3);
            Assert.Equal(m.M21, res.M21, 3);
            Assert.Equal(m.M22, res.M22, 3);
            Assert.Equal(m.M31, res.M31, 3);
            Assert.Equal(m.M32, res.M32, 3);
        }

        [Fact]
        public void Should_Generate_RotateTransform_minus_90_degrees()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            LayoutTransformControl lt = CreateWithChildAndMeasureAndTransform(
                                        100,
                                        25,
                                        new RotateTransform() { Angle = -90 });

            Assert.NotNull(lt.TransformRoot.RenderTransform);

            var m = lt.TransformRoot.RenderTransform.Value;

            var res = Matrix.CreateRotation(Matrix.ToRadians(-90));

            Assert.Equal(m.M11, res.M11, 3);
            Assert.Equal(m.M12, res.M12, 3);
            Assert.Equal(m.M21, res.M21, 3);
            Assert.Equal(m.M22, res.M22, 3);
            Assert.Equal(m.M31, res.M31, 3);
            Assert.Equal(m.M32, res.M32, 3);
        }

        [Fact]
        public void Should_Generate_ScaleTransform_x2()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            LayoutTransformControl lt = CreateWithChildAndMeasureAndTransform(
                                        100,
                                        50,
                                        new ScaleTransform() { ScaleX = 2, ScaleY = 2 });

            Assert.NotNull(lt.TransformRoot.RenderTransform);

            Matrix m = lt.TransformRoot.RenderTransform.Value;
            Matrix res = Matrix.CreateScale(2, 2);

            Assert.Equal(m.M11, res.M11, 3);
            Assert.Equal(m.M12, res.M12, 3);
            Assert.Equal(m.M21, res.M21, 3);
            Assert.Equal(m.M22, res.M22, 3);
            Assert.Equal(m.M31, res.M31, 3);
            Assert.Equal(m.M32, res.M32, 3);
        }

        [Fact]
        public void Should_Generate_SkewTransform_45_degrees()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            LayoutTransformControl lt = CreateWithChildAndMeasureAndTransform(
                                        100,
                                        100,
                                        new SkewTransform() { AngleX = 45, AngleY = 45 });

            Assert.NotNull(lt.TransformRoot.RenderTransform);

            Matrix m = lt.TransformRoot.RenderTransform.Value;

            Matrix res = Matrix.CreateSkew(Matrix.ToRadians(45), Matrix.ToRadians(45));

            Assert.Equal(m.M11, res.M11, 3);
            Assert.Equal(m.M12, res.M12, 3);
            Assert.Equal(m.M21, res.M21, 3);
            Assert.Equal(m.M22, res.M22, 3);
            Assert.Equal(m.M31, res.M31, 3);
            Assert.Equal(m.M32, res.M32, 3);
        }

        [Fact]
        public void Should_Generate_SkewTransform_minus_45_degrees()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            LayoutTransformControl lt = CreateWithChildAndMeasureAndTransform(
                                        100,
                                        100,
                                        new SkewTransform() { AngleX = -45, AngleY = -45 });

            Assert.NotNull(lt.TransformRoot.RenderTransform);

            Matrix m = lt.TransformRoot.RenderTransform.Value;

            Matrix res = Matrix.CreateSkew(Matrix.ToRadians(-45), Matrix.ToRadians(-45));

            Assert.Equal(m.M11, res.M11, 3);
            Assert.Equal(m.M12, res.M12, 3);
            Assert.Equal(m.M21, res.M21, 3);
            Assert.Equal(m.M22, res.M22, 3);
            Assert.Equal(m.M31, res.M31, 3);
            Assert.Equal(m.M32, res.M32, 3);
        }

        private static void TransformMeasureSizeTest(Size size, Transform transform, Size expectedSize)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            LayoutTransformControl lt = CreateWithChildAndMeasureAndTransform(
                size.Width,
                size.Height,
                transform);

            Size outSize = lt.DesiredSize;

            Assert.Equal(outSize.Width, expectedSize.Width);
            Assert.Equal(outSize.Height, expectedSize.Height);
        }

        private static void TransformRootBoundsTest(Size size, Transform transform, Rect expectedBounds)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            LayoutTransformControl lt = CreateWithChildAndMeasureAndTransform(size.Width, size.Height, transform);

            Rect outBounds = lt.TransformRoot.Bounds;

            Assert.Equal(outBounds.X, expectedBounds.X);
            Assert.Equal(outBounds.Y, expectedBounds.Y);
            Assert.Equal(outBounds.Width, expectedBounds.Width);
            Assert.Equal(outBounds.Height, expectedBounds.Height);
        }

        private static LayoutTransformControl CreateWithChildAndMeasureAndTransform(
                                                double width,
                                                double height,
                                                Transform transform)
        {
            var lt = new LayoutTransformControl()
            {
                LayoutTransform = transform
            };

            lt.Child = new Rectangle() { Width = width, Height = height };

            lt.Measure(Size.Infinity);
            lt.Arrange(new Rect(lt.DesiredSize));

            return lt;
        }
    }
}
