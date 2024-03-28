using Avalonia.Media;
using CrossUI;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;
#elif AVALONIA_D2D
namespace Avalonia.Direct2D1.RenderTests;
#else
namespace Avalonia.RenderTests.WpfCompare;
#endif


public class CrossTileBrushTests : CrossTestBase
{
    public CrossTileBrushTests() : base("Media/TileBrushes")
    {
    }

    [CrossFact]
    public void Simple_Checkboard_Pattern_Is_Rendered_Identically()
    {
        RenderAndCompare(new CrossControl()
        {
            Width = 100,
            Height = 100,
            Background = new CrossDrawingBrush()
            {
                Drawing = new CrossDrawingGroup()
                {
                    Children =
                    {
                        new CrossGeometryDrawing(new CrossRectangleGeometry(new(0, 0, 20, 20)))
                        {
                            Brush = new CrossSolidColorBrush(Colors.White)
                        },
                        new CrossGeometryDrawing(new CrossRectangleGeometry(new(0, 0, 10, 10)))
                        {
                            Brush = new CrossSolidColorBrush(Colors.Black)
                        },
                        new CrossGeometryDrawing(new CrossRectangleGeometry(new(10, 10, 10, 10)))
                        {
                            Brush = new CrossSolidColorBrush(Colors.Black)
                        },
                    }
                },
                Viewport = new Rect(0, 0, 10, 10),
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile
            }
        });

    }
}
