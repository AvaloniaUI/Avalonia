using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Shapes
{
    public class RectangleTests
    {
        [Fact]
        public void Measure_Does_Not_Set_RenderedGeometry_Rect()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Rectangle();

            target.Measure(new Size(100, 100));

            var geometry = Assert.IsType<RectangleGeometry>(target.RenderedGeometry);
            Assert.Equal(default, geometry.Rect);
        }

        [Fact]
        public void Arrange_Sets_RenderedGeometry_Rect()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Rectangle();

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var geometry = Assert.IsType<RectangleGeometry>(target.RenderedGeometry);
            Assert.Equal(new Rect(0, 0, 100, 100), geometry.Rect);
        }

        [Fact]
        public void Rearranging_Updates_RenderedGeometry_Rect()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var target = new Rectangle();

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var geometry = Assert.IsType<RectangleGeometry>(target.RenderedGeometry);
            Assert.Equal(new Rect(0, 0, 100, 100), geometry.Rect);

            target.Measure(new Size(200, 200));
            target.Arrange(new Rect(0, 0, 200, 200));

            geometry = Assert.IsType<RectangleGeometry>(target.RenderedGeometry);
            Assert.Equal(new Rect(0, 0, 200, 200), geometry.Rect);
        }

        [Fact]
        public void Changing_Fill_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new Rectangle()
            {
                Fill = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.Invocations.Clear();

            ((SolidColorBrush)target.Fill).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }

        [Fact]
        public void Changing_Stroke_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new Rectangle()
            {
                Stroke = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.Invocations.Clear();

            ((SolidColorBrush)target.Stroke).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }
    }
}
