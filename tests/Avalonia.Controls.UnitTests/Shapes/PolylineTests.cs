using System.Collections.ObjectModel;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Shapes;

public class PolylineTests : ScopedTestBase
{
    [Fact]
    public void Polyline_Will_Update_Geometry_On_Shapes_Collection_Content_Change()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var points = new ObservableCollection<Point>();

        var target = new Polyline { Points = points };
        target.Measure(new Size());
        Assert.True(target.IsMeasureValid);
        
        var root = new TestRoot(target);

        points.Add(new Point());

        Assert.False(target.IsMeasureValid);

        root.Child = null;
    }

    [Fact]
    public void FillRule_On_Polyline_Is_Applied_To_DefiningGeometry()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var target = new Polyline
        {
            Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
            FillRule = FillRule.NonZero
        };

        target.Measure(Size.Infinity);

        var geometry = Assert.IsType<PolylineGeometry>(target.DefiningGeometry);
        Assert.Equal(FillRule.NonZero, geometry.FillRule);
    }
}
