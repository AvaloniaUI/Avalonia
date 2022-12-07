using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class HitTesting
    {
        [Fact]
        public void Hit_Test_Should_Respect_Fill()
        {
            using (AvaloniaLocator.EnterScope())
            {
                SkiaPlatform.Initialize();

                var root = new TestRoot
                {
                    Width = 100,
                    Height = 100,
                    Child = new Ellipse
                    {
                        Width = 100,
                        Height = 100,
                        Fill = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                root.Renderer = new DeferredRenderer((IRenderRoot)root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var outsideResult = root.Renderer.HitTest(new Point(10, 10), root, null);
                var insideResult = root.Renderer.HitTest(new Point(50, 50), root, null);

                Assert.Empty(outsideResult);
                Assert.Equal(new[] { root.Child }, insideResult);
            }
        }

        [Fact]
        public void Hit_Test_Should_Respect_Stroke()
        {
            using (AvaloniaLocator.EnterScope())
            {
                SkiaPlatform.Initialize();

                var root = new TestRoot
                {
                    Width = 100,
                    Height = 100,
                    Child = new Ellipse
                    {
                        Width = 100,
                        Height = 100,
                        Stroke = Brushes.Red,
                        StrokeThickness = 5,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                root.Renderer = new DeferredRenderer((IRenderRoot)root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var outsideResult = root.Renderer.HitTest(new Point(50, 50), root, null);
                var insideResult = root.Renderer.HitTest(new Point(1, 50), root, null);

                Assert.Empty(outsideResult);
                Assert.Equal(new[] { root.Child }, insideResult);
            }
        }
    }
}
