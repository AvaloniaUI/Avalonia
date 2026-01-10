using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class PolygonTests : TestBase
    {
        public PolygonTests()
            : base(@"Shapes\Polygon")
        {
        }
        
        [Theory]
        [InlineData(FillRule.EvenOdd)]
        [InlineData(FillRule.NonZero)]
        public async Task Polygon_FillRule(FillRule fillRule)
        {
            var target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 220,
                Height = 220,
                Child = new Polygon
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.Gold,
                    Points = new Points
                    {
                        new Point(50, 0),
                        new Point(21, 90),
                        new Point(98, 35),
                        new Point(2, 35),
                        new Point(79, 90)
                    },
                    Stretch = Stretch.Uniform,
                    FillRule = fillRule
                }
            };

            var testName = $"{nameof(Polygon_FillRule)}_{fillRule}";
            await RenderToFile(target, testName);
            CompareImages(testName);
        }

        [Fact]
        public async Task Polygon_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Polygon
                {
                    Stroke = Brushes.DarkBlue,
                    Stretch = Stretch.Uniform,
                    Fill = Brushes.Violet,
                    Points = new Points { new Point(5, 0), new Point(8, 8), new Point(0, 3), new Point(10, 3), new Point(2, 8) },
                    StrokeThickness = 1
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Polygon_NonUniformFill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 400,
                Height = 200,
                Child = new Polygon
                {
                    Stroke = Brushes.DarkBlue,
                    Stretch = Stretch.Fill,
                    Fill = Brushes.Violet,
                    Points = new Points { new Point(5, 0), new Point(8, 8), new Point(0, 3), new Point(10, 3), new Point(2, 8) },
                    StrokeThickness = 5,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
