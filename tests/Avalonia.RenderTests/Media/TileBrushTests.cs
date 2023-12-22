using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;
#else
namespace Avalonia.Direct2D1.RenderTests.Media;
#endif
public class DrawingBrushTests: TestBase
{
    public DrawingBrushTests()
        : base(@"Media\DrawingBrush")
    {
    }
    
    [Fact]
    public async Task DrawingBrushIsProperlyTiled()
    {
        Decorator target = new Decorator
        {
            Padding = new Thickness(10),
            Width = 220,
            Height = 220,
            Child = new Rectangle
            {
                Fill = new DrawingBrush
                {
                    Stretch = Stretch.None,
                    TileMode = TileMode.Tile,
                    Drawing = CreateDrawing(),
                    DestinationRect = new RelativeRect(0,0,0.25,0.25, RelativeUnit.Relative)
                }
            }
        };

        await RenderToFile(target);
        CompareImages();
    }
    

#if AVALONIA_SKIA
    [Fact]
    public async Task DrawingBrushIsProperlyUpscaled()
    {
        Decorator target = new Decorator
        {
            Padding = new Thickness(10),
            Width = 420,
            Height = 420,
            Child = new Rectangle
            {
                Fill = new DrawingBrush
                {
                    Stretch = Stretch.Fill,
                    TileMode = TileMode.None,
                    Drawing = CreateDrawing()
                }
            }
        };

        await RenderToFile(target);
        CompareImages();
    }
#endif

    GeometryDrawing CreateDrawing()
    {
        return new GeometryDrawing
        {
            Geometry = new GeometryGroup
            {
                Children =
                {
                    new RectangleGeometry(new Rect(50, 25, 25, 25)),
                    new RectangleGeometry(new Rect(25, 50, 25, 25)),
                }
            },
            Pen = new Pen(new LinearGradientBrush()
            {
                GradientStops =
                {
                    new GradientStop(Colors.Blue, 0),
                    new GradientStop(Colors.Black, 1),
                }
            }, 5),
            Brush = Brushes.Yellow,
        };
    }
    
}
