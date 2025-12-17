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
            Fill = Brushes.Red,
            FillRule = FillRule.NonZero
        };

        target.Measure(Size.Infinity);

        var geometry = Assert.IsType<PolylineGeometry>(target.DefiningGeometry);
        Assert.Equal(FillRule.NonZero, geometry.FillRule);
        Assert.True(geometry.IsFilled);
    }

    [Fact]
    public void FillRule_Differs_Between_EvenOdd_And_NonZero()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var evenOdd = new Polyline
        {
            Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
            Fill = Brushes.Red,
            FillRule = FillRule.EvenOdd
        };

        var nonZero = new Polyline
        {
            Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
            Fill = Brushes.Red,
            FillRule = FillRule.NonZero
        };

        evenOdd.Measure(Size.Infinity);
        nonZero.Measure(Size.Infinity);

        Assert.Equal(FillRule.EvenOdd, Assert.IsType<PolylineGeometry>(evenOdd.DefiningGeometry).FillRule);
        Assert.Equal(FillRule.NonZero, Assert.IsType<PolylineGeometry>(nonZero.DefiningGeometry).FillRule);
    }

    [Fact]
    public void When_Fill_Is_Null_Polyline_Geometry_Is_Not_Filled()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var target = new Polyline
        {
            Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
            FillRule = FillRule.NonZero,
            Fill = null
        };

        target.Measure(Size.Infinity);
        var geometry = Assert.IsType<PolylineGeometry>(target.DefiningGeometry);
        Assert.False(geometry.IsFilled);
    }
}
