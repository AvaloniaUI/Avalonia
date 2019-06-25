using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Visuals.UnitTests
{
    public class VisualExtensionsTests
    {
        [Fact]
        public void TranslatePoint_Should_Respect_RenderTransforms()
        {
            Border target;
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = new Decorator
                {
                    Width = 50,
                    Height = 50,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RenderTransform = new TranslateTransform(25, 25),
                    Child = target = new Border(),
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var result = target.TranslatePoint(new Point(0, 0), root);

            Assert.Equal(new Point(50, 50), result);
        }
    }
}
