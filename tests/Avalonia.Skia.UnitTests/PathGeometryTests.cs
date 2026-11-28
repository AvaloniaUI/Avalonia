using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class PathGeometryTests
{
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    public void Open_Keeps_Content_When_Transform_Is_Set(bool curved, bool stroked, bool grouped)
    {
        using var app = UnitTestApplication.Start(
            TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface()));

        var geometry = new PathGeometry { Transform = new TranslateTransform(10, 20) };

        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(20, 20), true);
            if (curved)
                context.QuadraticBezierTo(new Point(100, 20), new Point(100, 100), stroked);
            else
            {
                context.LineTo(new Point(100, 20));
                context.LineTo(new Point(100, 100));
            }
            context.EndFigure(true);
        }

        Geometry result = grouped ? new GeometryGroup { Children = { geometry } } : geometry;
        HitTesting.AssertGeometryCanBeShared(result.PlatformImpl, new Point(90, 70));
        Assert.Equal(new Rect(30, 40, 80, 80), result.Bounds);
    }
}
