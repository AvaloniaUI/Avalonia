using Perspex.Controls.Presenters;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Media;
using Xunit;

namespace Perspex.Controls.UnitTests
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
        public void Should_Generate_RenderTransform_90_degrees()
        {
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
        public void Should_Generate_RenderTransform_minus_90_degrees()
        {
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

        private static void TransformMeasureSizeTest(Size size, Transform transform, Size expectedSize)
        {
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
                LayoutTransform = transform,
                Template = new FuncControlTemplate<LayoutTransformControl>(
                                p => new ContentPresenter() { Content = p.Content })
            };

            lt.Content = new Rectangle() { Width = width, Height = height };

            lt.ApplyTemplate();

            //we need to force create visual child
            //so the measure after is correct
            (lt.Presenter as ContentPresenter).UpdateChild();

            Assert.NotNull(lt.Presenter?.Child);

            lt.Measure(Size.Infinity);
            lt.Arrange(new Rect(lt.DesiredSize));

            return lt;
        }
    }
}