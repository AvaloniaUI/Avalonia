using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using CrossUI;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;
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

    // Scaling about the centre of the 200x200 canvas rather than about its origin, so the
    // transformed tile stays inside the fill and the output shows where it landed.
    private static readonly Matrix s_halveAboutCanvasCentre =
        Matrix.CreateTranslation(-100, -100)
        * Matrix.CreateScale(0.5, 0.5)
        * Matrix.CreateTranslation(100, 100);

    [CrossFact]
    public void Should_Render_Drawing_Brush_With_Transform_On_An_Offset_Fill()
    {
        // A relative viewport lays the tile over the fill, and the transform then acts on it in
        // target space, so the crimson rect ends up centred in the fill at half its size.
        // Transforming the tile before it is positioned would push it towards the bottom edge.
        var brush = new CrossDrawingBrush
        {
            TileMode = TileMode.None,
            Transform = s_halveAboutCanvasCentre,
            Drawing = new CrossGeometryDrawing(new CrossRectangleGeometry(new Rect(0, 0, 100, 100)))
            {
                Brush = new CrossSolidColorBrush(Colors.Crimson)
            }
        };

        RenderAndCompare(new CrossFuncControl(ctx => ctx.DrawRectangle(brush, null, new Rect(40, 70, 120, 60)))
        {
            Width = 200,
            Height = 200,
            Background = new CrossSolidColorBrush(Colors.White)
        });
    }

    [CrossFact]
    public void Should_Render_Image_Brush_With_Transform_On_An_Offset_Fill()
    {
        var path = Path.Join(
            Path.GetDirectoryName(typeof(CrossTileBrushTests).Assembly.Location), "Assets", "Ramp64.png");

        var brush = new CrossImageBrush { Path = path, Transform = s_halveAboutCanvasCentre };

        RenderAndCompare(new CrossFuncControl(ctx => ctx.DrawRectangle(brush, null, new Rect(40, 70, 120, 60)))
        {
            Width = 200,
            Height = 200,
            Background = new CrossSolidColorBrush(Colors.White)
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
