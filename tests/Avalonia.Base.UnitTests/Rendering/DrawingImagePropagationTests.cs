using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class DrawingImagePropagationTests : CompositorTestsBase
{
    private static Image CreateImage(DrawingImage image) => new()
    {
        Source = image,
        Width = 20,
        Height = 10,
        [Canvas.LeftProperty] = 30,
        [Canvas.TopProperty] = 50
    };

    [Fact]
    public void Mutating_Geometry_Inside_DrawingImage_Invalidates_Consumer()
    {
        using var services = new CompositorCanvas();

        var geometry = new RectangleGeometry(new Rect(0, 0, 20, 10));
        var image = new DrawingImage(new GeometryDrawing { Brush = Brushes.Red, Geometry = geometry });
        services.Canvas.Children.Add(CreateImage(image));
        services.RunJobs();
        services.Events.Rects.Clear();

        geometry.Rect = new Rect(0, 0, 30, 15);

        services.AssertRects(
            new Rect(30, 50, 20, 10),
            new Rect(30, 50, 30, 15));
    }
}
