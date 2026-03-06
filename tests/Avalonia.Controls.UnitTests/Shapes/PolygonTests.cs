using System.Collections.ObjectModel;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Shapes;

public class PolygonTests : ScopedTestBase
{
    [Fact]
    public void Polygon_Will_Update_Geometry_On_Shapes_Collection_Content_Change()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var points = new ObservableCollection<Point>();

        var target = new Polygon() { Points = points };
        target.Measure(new Size());
        Assert.True(target.IsMeasureValid);

        var root = new TestRoot(target);

        points.Add(new Point());

        Assert.False(target.IsMeasureValid);

        root.Child = null;
    }

    [Fact]
    public void FillRule_On_Polygon_Is_Applied_To_DefiningGeometry()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var target = new Polygon
        {
            Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
            FillRule = FillRule.NonZero
        };

        target.Measure(Size.Infinity);

        var geometry = Assert.IsType<PolylineGeometry>(target.DefiningGeometry);
        Assert.Equal(FillRule.NonZero, geometry.FillRule);
    }

    [Fact]
    public void Polygon_Equals_Closed_Polyline_Bounds()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var polyline = new Polyline
        {
            Points = new Points
            {
                new Point(0, 0),
                new Point(10, 0),
                new Point(10, 10),
                new Point(0, 10),
                new Point(0, 0)
            },
            FillRule = FillRule.NonZero
        };

        var polygon = new Polygon
        {
            Points = new Points { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10) },
            FillRule = FillRule.NonZero
        };

        polyline.Measure(Size.Infinity);
        polygon.Measure(Size.Infinity);

        Assert.Equal(polygon.DefiningGeometry!.Bounds, polyline.DefiningGeometry!.Bounds);
    }
}
