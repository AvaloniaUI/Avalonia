using System.Collections.Generic;
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

    [CrossFact]
    public void Should_Render_Scaled_TileBrush()
    {
        var brush = new CrossDrawingBrush
        {
            TileMode = TileMode.Tile,
            Viewbox = new Rect(0, 0, 20, 20),
            ViewboxUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, 20, 20),
            ViewportUnits = BrushMappingMode.Absolute,
            Drawing = new CrossGeometryDrawing(new CrossSvgGeometry("M 0 0 l 50 50"))
            {
                Pen = new CrossPen { Brush = new CrossSolidColorBrush(Colors.Red), Thickness = 5 }
            }
        };

        RenderAndCompare(new CrossControl()
        {
            Width = 100,
            Height = 100,
            Background = brush
        });

    }

    [CrossFact]
    public void Should_Render_Aligned_TileBrush()
    {
        var brush = new CrossDrawingBrush
        {
            TileMode = TileMode.Tile,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center,
            Stretch = Stretch.Uniform,
            Drawing = new CrossDrawingGroup()
            {
                Children = new List<CrossDrawing>()
                {
                    new CrossGeometryDrawing(new CrossRectangleGeometry(new(0, 0, 100, 150)))
                    {
                        Brush = new CrossSolidColorBrush(Colors.Crimson)
                    },
                }
            }
        };

        RenderAndCompare(new CrossControl()
        {
            Width = 100,
            Height = 100,
            Background = brush
        });

    }

    [CrossFact]
    public void Should_Render_TileBrush_With_TileMode_None()
    {
        var brush = new CrossDrawingBrush
        {
            TileMode = TileMode.None,
            Stretch = Stretch.Fill,
            Viewbox = new Rect(0, 0, 50, 50),
            ViewboxUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, 50, 50),
            ViewportUnits = BrushMappingMode.Absolute,
            Drawing = new CrossDrawingGroup()
            {
                Children = new List<CrossDrawing>()
                {
                    new CrossGeometryDrawing(new CrossRectangleGeometry(new(0, 0, 50, 50)))
                    {
                        Brush = new CrossSolidColorBrush(Colors.Crimson)
                    },
                }
            }
        };

        RenderAndCompare(new CrossControl()
        {
            Width = 200,
            Height = 200,
            Background = brush
        });

    }

    [CrossFact]
    public void Should_Render_With_Transform()
    {
        var brush = new CrossDrawingBrush()
        {
            TileMode = TileMode.None,
            Viewbox = new Rect(0, 0, 1, 1),
            ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
            Viewport = new Rect(0, 0, 50, 50),
            ViewportUnits = BrushMappingMode.Absolute,
            Transform = Matrix.CreateTranslation(150, 150),
            Drawing = new CrossDrawingGroup()
            {
                Children = new List<CrossDrawing>()
                {
                    new CrossGeometryDrawing(new CrossRectangleGeometry(new(0, 0, 100, 100)))
                    {
                        Brush = new CrossSolidColorBrush(Colors.Crimson)
                    },
                    new CrossGeometryDrawing(new CrossRectangleGeometry(new(20, 20, 60, 60)))
                    {
                        Brush = new CrossSolidColorBrush(Colors.Blue)
                    }
                }
            }
        };

        RenderAndCompare(new CrossControl()
        {
            Width = 200,
            Height = 200,
            Background = brush
        });
    }
}
