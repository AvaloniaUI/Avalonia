using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ViewboxTests
    {
        [Fact]
        public void Viewbox_Stretch_Uniform_Child()
        {
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
            var target = new Viewbox() { Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, double.PositiveInfinity));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(200, 100), target.DesiredSize);
            var scaleTransform = target.Child.RenderTransform as ScaleTransform;

            Assert.NotNull(scaleTransform);
            Assert.Equal(2.0, scaleTransform.ScaleX);
            Assert.Equal(2.0, scaleTransform.ScaleY);
        }
    }
}
