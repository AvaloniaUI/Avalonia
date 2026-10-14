using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class PathGeometryTests
{
    [Fact]
    public void PathGeometry_Triggers_Invalidation_On_Figures_Add()
    {
        var segment = new PolyLineSegment()
        {
            Points = [new Point(1, 1), new Point(2, 2)]
        };

        var figure = new PathFigure()
        {
            Segments = [segment],
            IsClosed = false,
            IsFilled = false,
        };
        
        var target = new PathGeometry();

        var changed = false;

        target.Changed += (_, _) => { changed = true; };
        
        target.Figures?.Add(figure);
        Assert.True(changed);
    }

    [Fact]
    public void Open_Adds_To_Figures()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var target = new PathGeometry();

        using (var context = target.Open())
        {
            context.BeginFigure(new Point(0, 0), true);
            context.LineTo(new Point(10, 0));
            context.EndFigure(true);
        }

        Assert.Equal(1, target.Figures!.Count);
    }

    [Fact]
    public void Open_Adds_To_Figures_When_A_Transform_Is_Set()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var target = new PathGeometry { Transform = new TranslateTransform(10, 20) };

        using (var context = target.Open())
        {
            context.BeginFigure(new Point(0, 0), true);
            context.LineTo(new Point(10, 0));
            context.EndFigure(true);
        }

        Assert.Equal(1, target.Figures!.Count);
    }
}
