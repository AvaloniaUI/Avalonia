using System.Collections.ObjectModel;
using Avalonia.Controls.Shapes;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Shapes;

public class PolygonTests
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
}
