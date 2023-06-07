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
    public class PolylineTests : TestBase
    {
        public PolylineTests()
            : base(@"Shapes\Polyline")
        {
        }

        [Fact]
        public async Task Polyline_1px_Stroke()
        {
            var polylinePoints = new Points { new Point(0, 0), new Point(5, 0), new Point(6, -2), new Point(7, 3), new Point(8, -3),
                new Point(9, 1), new Point(10, 0), new Point(15, 0) };

            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 400,
                Height = 200,
                Child = new Polyline
                {
                    Stroke = Brushes.Brown,
                    Points = polylinePoints,
                    Stretch = Stretch.Uniform,
                    StrokeThickness = 1
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task Polyline_10px_Stroke_PenLineJoin()
        {
            var polylinePoints = new Points { new Point(0, 0), new Point(5, 0), new Point(6, -2), new Point(7, 3), new Point(8, -3),
                new Point(9, 1), new Point(10, 0), new Point(15, 0) };

            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 400,
                Height = 200,
                Child = new Polyline
                {
                    Stroke = Brushes.Brown,
                    Points = polylinePoints,
                    Stretch = Stretch.Uniform,
                    StrokeJoin = PenLineJoin.Round,
                    StrokeLineCap = PenLineCap.Round,
                    StrokeThickness = 10
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
