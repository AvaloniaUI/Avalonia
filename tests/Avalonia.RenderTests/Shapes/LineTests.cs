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
    public class LineTests : TestBase
    {
        public LineTests()
            : base(@"Shapes\Line")
        {
        }
        
        [Fact]
        public async Task Line_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(200, 200)
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task Line_1px_Stroke_Reversed()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StartPoint = new Point(200, 0),
                    EndPoint = new Point(0, 200)
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Line_1px_Stroke_Vertical()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StartPoint = new Point(100, 200),
                    EndPoint = new Point(100, 0)
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
