using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
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


public class CrossGeometryTests : CrossTestBase
{
    public CrossGeometryTests() : base("Media/Geometry")
    {
    }

    [CrossFact]
    public void Should_Render_Stream_Geometry()
    {
        var geometry = new CrossStreamGeometry();

        var context = geometry.GetContext();
        context.BeginFigure(new Point(150, 15), true, true);
        context.LineTo(new Point(258, 77), true);
        context.LineTo(new Point(258, 202), true);
        context.LineTo(new Point(150, 265), true);
        context.LineTo(new Point(42, 202), true);
        context.LineTo(new Point(42, 77), true);
        context.EndFigure();

        var brush = new CrossDrawingBrush()
        {
            TileMode = TileMode.None,
            Drawing = new CrossDrawingGroup()
            {
                Children = new List<CrossDrawing>()
                {
                    new CrossGeometryDrawing(new CrossRectangleGeometry(new(0, 0, 300, 280)))
                    {
                        Brush = new CrossSolidColorBrush(Colors.White)
                    },
                    new CrossGeometryDrawing(geometry)
                    {
                        Pen = new CrossPen()
                        {
                            Brush = new CrossSolidColorBrush(Colors.Black),
                            Thickness = 2
                        }
                    }
                }
            }
        };

        RenderAndCompare(new CrossControl()
        {
            Width = 300,
            Height = 280,
            Background = brush
        });
    }

    [CrossFact]
    public void Should_Render_Geometry_With_Strokeless_Lines()
    {
        var geometry = new CrossStreamGeometry();

        var context = geometry.GetContext();
        context.BeginFigure(new Point(150, 15), true, true);
        context.LineTo(new Point(258, 77), true);
        context.LineTo(new Point(258, 202), false);
        context.LineTo(new Point(150, 265), true);
        context.LineTo(new Point(42, 202), true);
        context.LineTo(new Point(42, 77), false);
        context.EndFigure();

        var brush = new CrossDrawingBrush()
        {
            TileMode = TileMode.None,
            Drawing = new CrossDrawingGroup()
            {
                Children = new List<CrossDrawing>()
                {
                    new CrossGeometryDrawing(new CrossRectangleGeometry(new(0, 0, 300, 280)))
                    {
                        Brush = new CrossSolidColorBrush(Colors.White)
                    },
                    new CrossGeometryDrawing(geometry)
                    {
                        Pen = new CrossPen()
                        {
                            Brush = new CrossSolidColorBrush(Colors.Black),
                            Thickness = 2
                        }
                    }
                }
            }
        };

        RenderAndCompare(new CrossControl()
        {
            Width = 300,
            Height = 280,
            Background = brush
        });
    }


    [CrossFact]
    public void Should_Render_PolyLineSegment_With_Strokeless_Lines()
    {
        var brush = new CrossSolidColorBrush(Colors.Blue);
        var pen = new CrossPen()
        {
            Brush = new CrossSolidColorBrush(Colors.Red),
            Thickness = 8
        };
        var figure = new CrossPathFigure()
        {
            Closed = true,
            Segments =
            {
                new CrossPathSegment.PolyLine([new(0, 0), new(100, 0), new(100, 100), new(0, 100), new(0, 0)], false)
            }
        };
        var geometry = new CrossPathGeometry { Figures = { figure } };

        var control = new CrossFuncControl(ctx => ctx.DrawGeometry(brush, pen, geometry))
        { 
            Width = 100, 
            Height = 100,
        };

        RenderAndCompare(control,
            $"{nameof(Should_Render_PolyLineSegment_With_Strokeless_Lines)}");
    }

    // Skip the test for now
#if !AVALONIA_SKIA
    [CrossTheory,
        InlineData(PenLineCap.Flat, PenLineJoin.Round),
        InlineData(PenLineCap.Flat, PenLineJoin.Bevel),
        InlineData(PenLineCap.Flat, PenLineJoin.Miter),
        InlineData(PenLineCap.Round, PenLineJoin.Round),
        InlineData(PenLineCap.Round, PenLineJoin.Bevel),
        InlineData(PenLineCap.Round, PenLineJoin.Miter),
    ]
    public void Should_Properly_CloseFigure(PenLineCap lineCap, PenLineJoin lineJoin)
    {
        var geometry = new CrossPathGeometry();


        var center = new Point(150, 150);
        var r = 100d;

        var pointCount = 5;
        var points = Enumerable.Range(0, pointCount).Select(a => a * Math.PI / pointCount * 2).Select(a =>
            new Point(center.X + Math.Sin(a) * r, center.Y + Math.Cos(a) * r)).ToArray();

        var figure = new CrossPathFigure() { Start = points[0], Closed = true };
        geometry.Figures.Add(figure);
        var lineNum = 0;
        for (var c = 2; lineNum < pointCount - 1; c = (c + 2) % pointCount, lineNum++)
        {
            figure.Segments.Add(new CrossPathSegment.Line(points[c], (lineNum) % 3 < 2));
        }
        
        var control = new CrossFuncControl(ctx =>
        {
            ctx.DrawRectangle(new CrossSolidColorBrush(Colors.White), null, new(0, 0, 300, 300));
            ctx.DrawGeometry(null,
                new CrossPen()
                {
                    Brush = new CrossSolidColorBrush(Colors.Black),
                    Thickness = 20,
                    LineJoin = lineJoin,
                    LineCap = lineCap
                }, geometry);
        }) { Width = 300, Height = 300 };
        RenderAndCompare(control,
            $"{nameof(Should_Properly_CloseFigure)}_{lineCap}_{lineJoin}");
    }
#endif
}
