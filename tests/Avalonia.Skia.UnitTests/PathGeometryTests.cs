using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class PathGeometryTests
{
    [Fact]
    public void Open_Keeps_Content_When_Transform_Is_Set()
    {
        using var app = UnitTestApplication.Start(
            TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface()));

        var geometry = new PathGeometry { Transform = new TranslateTransform(10, 20) };

        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(20, 20), true);
            context.LineTo(new Point(100, 20));
            context.LineTo(new Point(100, 100));
            context.EndFigure(true);
        }

        Assert.Equal(new Rect(30, 40, 80, 80), geometry.Bounds);
    }
}
