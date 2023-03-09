using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.LogicalTree;
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
            
            Assert.True(TryGetScale(target, out Vector scale));
            Assert.Equal(2.0, scale.X);
            Assert.Equal(2.0, scale.Y);
        }

        [Fact]
        public void Viewbox_Stretch_None_Child()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Stretch = Stretch.None, Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(100, 50), target.DesiredSize);
            
            Assert.True(TryGetScale(target, out Vector scale));
            Assert.Equal(1.0, scale.X);
            Assert.Equal(1.0, scale.Y);
        }

        [Fact]
        public void Viewbox_Stretch_Fill_Child()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Stretch = Stretch.Fill, Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(200, 200), target.DesiredSize);
            
            Assert.True(TryGetScale(target, out Vector scale));
            Assert.Equal(2.0, scale.X);
            Assert.Equal(4.0, scale.Y);
        }

        [Fact]
        public void Viewbox_Stretch_UniformToFill_Child()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Stretch = Stretch.UniformToFill, Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(200, 200), target.DesiredSize);
            
            Assert.True(TryGetScale(target, out Vector scale));
            Assert.Equal(4.0, scale.X);
            Assert.Equal(4.0, scale.Y);
        }

        [Fact]
        public void Viewbox_Stretch_Uniform_Child_With_Unrestricted_Width()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(double.PositiveInfinity, 200));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(400, 200), target.DesiredSize);
            
            Assert.True(TryGetScale(target, out Vector scale));
            Assert.Equal(4.0, scale.X);
            Assert.Equal(4.0, scale.Y);
        }

        [Fact]
        public void Viewbox_Stretch_Uniform_Child_With_Unrestricted_Height()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Viewbox() { Child = new Rectangle() { Width = 100, Height = 50 } };

            target.Measure(new Size(200, double.PositiveInfinity));
            target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

            Assert.Equal(new Size(200, 100), target.DesiredSize);
            
            Assert.True(TryGetScale(target, out Vector scale));
            Assert.Equal(2.0, scale.X);
            Assert.Equal(2.0, scale.Y);
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

            Assert.True(TryGetScale(target, out Vector scale));
            Assert.Equal(expectedScale, scale.X);
            Assert.Equal(expectedScale, scale.Y);
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

            Assert.True(TryGetScale(target, out Vector scale));
            Assert.Equal(expectedScale, scale.X);
            Assert.Equal(expectedScale, scale.Y);
        }

        [Fact]
        public void Child_Should_Be_Logical_Child_Of_Viewbox()
        {
            var target = new Viewbox();

            Assert.Empty(target.GetLogicalChildren());

            var child = new Canvas();
            target.Child = child;

            Assert.Single(target.GetLogicalChildren(), child);
            Assert.Same(child.GetLogicalParent(), target);

            target.Child = null;

            Assert.Empty(target.GetLogicalChildren());
            Assert.Null(child.GetLogicalParent());
        }

        [Fact]
        public void Changing_Child_Should_Invalidate_Layout()
        {
            var target = new Viewbox();

            target.Child = new Canvas
            {
                Width = 100,
                Height = 100,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));
            Assert.Equal(new Size(100, 100), target.DesiredSize);

            target.Child = new Canvas
            {
                Width = 200,
                Height = 200,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));
            Assert.Equal(new Size(200, 200), target.DesiredSize);
        }

        [Fact]
        public void Child_DataContext_Binding_Works()
        {
            var data = new
            {
                Foo = "foo",
            };

            var target = new Viewbox()
            {
                DataContext = data,
                Child = new Canvas
                {
                    [!Canvas.DataContextProperty] = new Binding("Foo"),
                },
            };

            Assert.Equal("foo", target.Child.DataContext);
        }
      
        private static bool TryGetScale(Viewbox viewbox, out Vector scale)
        {
            if (viewbox.InternalTransform is null)
            {
                scale = default;
                return false;
            }

            var matrix = viewbox.InternalTransform.Value;

            Matrix.TryDecomposeTransform(matrix, out var decomposed);

            scale = decomposed.Scale;
            return true;
        }
    }
}
