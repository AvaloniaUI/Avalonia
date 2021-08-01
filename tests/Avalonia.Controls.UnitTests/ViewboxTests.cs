using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ViewboxTests
    {
        [Fact]
        public void Viewbox_Stretch_Uniform_Child()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(200, 100), target.DesiredSize);
            var scaleTransform = target.Child.RenderTransform as ScaleTransform;

            Assert.NotNull(scaleTransform);
            Assert.Equal(2.0, scaleTransform.ScaleX);
            Assert.Equal(2.0, scaleTransform.ScaleY);
        }

        [Fact]
        public void Viewbox_Stretch_None_Child()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Stretch = Stretch.None, Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(100, 50), target.DesiredSize);
            var scaleTransform = target.Child.RenderTransform as ScaleTransform;

            Assert.NotNull(scaleTransform);
            Assert.Equal(1.0, scaleTransform.ScaleX);
            Assert.Equal(1.0, scaleTransform.ScaleY);
        }

        [Fact]
        public void Viewbox_Stretch_Fill_Child()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Stretch = Stretch.Fill, Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(200, 200), target.DesiredSize);
            var scaleTransform = target.Child.RenderTransform as ScaleTransform;

            Assert.NotNull(scaleTransform);
            Assert.Equal(2.0, scaleTransform.ScaleX);
            Assert.Equal(4.0, scaleTransform.ScaleY);
        }

        [Fact]
        public void Viewbox_Stretch_UniformToFill_Child()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Stretch = Stretch.UniformToFill, Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(200, 200), target.DesiredSize);
            var scaleTransform = target.Child.RenderTransform as ScaleTransform;

            Assert.NotNull(scaleTransform);
            Assert.Equal(4.0, scaleTransform.ScaleX);
            Assert.Equal(4.0, scaleTransform.ScaleY);
        }

        [Fact]
        public void Viewbox_Stretch_Uniform_Child_With_Unrestricted_Width()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(double.PositiveInfinity, 200));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(400, 200), target.DesiredSize);
            var scaleTransform = target.Child.RenderTransform as ScaleTransform;

            Assert.NotNull(scaleTransform);
            Assert.Equal(4.0, scaleTransform.ScaleX);
            Assert.Equal(4.0, scaleTransform.ScaleY);
        }

        [Fact]
        public void Viewbox_Stretch_Uniform_Child_With_Unrestricted_Height()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, double.PositiveInfinity));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(200, 100), target.DesiredSize);
            var scaleTransform = target.Child.RenderTransform as ScaleTransform;

            Assert.NotNull(scaleTransform);
            Assert.Equal(2.0, scaleTransform.ScaleX);
            Assert.Equal(2.0, scaleTransform.ScaleY);
        }

        [Theory]
        [InlineData(50, 100, 50, 100, 50, 100, 1)]
        [InlineData(50, 100, 150, 150, 50, 100, 1)]
        [InlineData(50, 100, 25, 50, 25, 50, 0.5)]
        public void Viewbox_Should_Return_Correct_SizeAndScale_StretchDirection_DownOnly(
            double childWidth, double childHeight,
            double viewboxWidth, double viewboxHeight,
            double expectedWidth, double expectedHeight,
            double expectedScale)
        {
            var target = new Viewbox
            {
                Child = new Control { Width = childWidth, Height = childHeight },
                StretchDirection = StretchDirection.DownOnly
            };

            target.Measure(new Size(viewboxWidth, viewboxHeight));
            target.Arrange(new Rect(default, target.DesiredSize));

            Assert.Equal(new Size(expectedWidth, expectedHeight), target.DesiredSize);

            var scaleTransform = target.Child.RenderTransform as ScaleTransform;

            Assert.NotNull(scaleTransform);
            Assert.Equal(expectedScale, scaleTransform.ScaleX);
            Assert.Equal(expectedScale, scaleTransform.ScaleY);
        }

        [Theory]
        [InlineData(50, 100, 50, 100, 50, 100, 1)]
        [InlineData(50, 100, 25, 50, 25, 50, 1)]
        [InlineData(50, 100, 150, 150, 75, 150, 1.5)]
        public void Viewbox_Should_Return_Correct_SizeAndScale_StretchDirection_UpOnly(
            double childWidth, double childHeight,
            double viewboxWidth, double viewboxHeight,
            double expectedWidth, double expectedHeight,
            double expectedScale)
        {
            var target = new Viewbox
            {
                Child = new Control { Width = childWidth, Height = childHeight },
                StretchDirection = StretchDirection.UpOnly
            };

            target.Measure(new Size(viewboxWidth, viewboxHeight));
            target.Arrange(new Rect(default, target.DesiredSize));

            Assert.Equal(new Size(expectedWidth, expectedHeight), target.DesiredSize);

            var scaleTransform = target.Child.RenderTransform as ScaleTransform;

            Assert.NotNull(scaleTransform);
            Assert.Equal(expectedScale, scaleTransform.ScaleX);
            Assert.Equal(expectedScale, scaleTransform.ScaleY);
        }
    }
}
