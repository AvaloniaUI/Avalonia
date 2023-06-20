using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Shapes
#endif
{
    public class PolygonTests : TestBase
    {
        public PolygonTests()
            : base(@"Shapes\Polygon")
        {
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
