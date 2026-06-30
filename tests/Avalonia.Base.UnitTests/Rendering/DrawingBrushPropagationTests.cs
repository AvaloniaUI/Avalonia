using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class DrawingBrushPropagationTests : CompositorTestsBase
{
    private static Border CreateBorder(DrawingBrush brush) => new()
    {
        Background = brush,
        Width = 20,
        Height = 10,
        [Canvas.LeftProperty] = 30,
        [Canvas.TopProperty] = 50
    };

    [Fact]
    public void Mutating_Geometry_Inside_DrawingBrush_Invalidates_Consumer()
    {
        using var s = new CompositorCanvas();

        var geometry = new RectangleGeometry(new Rect(0, 0, 20, 10));
        var brush = new DrawingBrush(new GeometryDrawing { Brush = Brushes.Red, Geometry = geometry });
        s.Canvas.Children.Add(CreateBorder(brush));
        s.RunJobs();
        s.Events.Rects.Clear();

        geometry.Rect = new Rect(0, 0, 30, 15);

        s.AssertRects(new Rect(30, 50, 20, 10));
    }

    [Fact]
    public void Mutating_Brush_Inside_DrawingBrush_Invalidates_Consumer()
    {
        using var s = new CompositorCanvas();

        var fill = new SolidColorBrush(Colors.Red);
        var brush = new DrawingBrush(new GeometryDrawing
        {
            Brush = fill,
            Geometry = new RectangleGeometry(new Rect(0, 0, 20, 10))
        });
        s.Canvas.Children.Add(CreateBorder(brush));
        s.RunJobs();
        s.Events.Rects.Clear();

        fill.Color = Colors.Blue;

        s.AssertRects(new Rect(30, 50, 20, 10));
    }

    [Fact]
    public void Replacing_Drawing_Invalidates_Consumer()
    {
        using var s = new CompositorCanvas();

        var brush = new DrawingBrush(new GeometryDrawing
        {
            Brush = Brushes.Red,
            Geometry = new RectangleGeometry(new Rect(0, 0, 20, 10))
        });
        s.Canvas.Children.Add(CreateBorder(brush));
        s.RunJobs();
        s.Events.Rects.Clear();

        brush.Drawing = new GeometryDrawing
        {
            Brush = Brushes.Blue,
            Geometry = new RectangleGeometry(new Rect(0, 0, 20, 10))
        };

        s.AssertRects(new Rect(30, 50, 20, 10));
    }
}
