using System;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media;

public sealed class StreamGeometryTests
{
    [Fact]
    public void Open_Should_Append_To_Existing_Figures()
    {
        using var app = Start();

        var geometry = new StreamGeometry();
        AddLine(geometry, new Point(0, 0), new Point(10, 10));
        AddLine(geometry, new Point(20, 20), new Point(30, 30));

        Assert.Equal(new Rect(0, 0, 30, 30), geometry.Bounds);
    }

    [Fact]
    public void Open_Should_Raise_Changed()
    {
        using var app = Start();

        var geometry = new StreamGeometry();
        AddLine(geometry, new Point(0, 0), new Point(10, 10));
        var raised = false;
        geometry.Changed += (_, _) => raised = true;

        AddLine(geometry, new Point(20, 20), new Point(30, 30));

        Assert.True(raised);
    }

    [Fact]
    public void Open_Should_Update_Transformed_Geometry()
    {
        using var app = Start();

        var geometry = new StreamGeometry { Transform = new TranslateTransform(50, 50) };
        AddLine(geometry, new Point(0, 0), new Point(10, 10));
        Assert.Equal(new Rect(50, 50, 10, 10), geometry.Bounds);

        AddLine(geometry, new Point(20, 20), new Point(30, 30));
        Assert.Equal(new Rect(50, 50, 30, 30), geometry.Bounds);
    }

    [Fact]
    public void Open_Should_Update_ContourLength()
    {
        using var app = Start();

        var geometry = new StreamGeometry();
        Assert.Equal(0, geometry.ContourLength);

        AddLine(geometry, new Point(0, 0), new Point(100, 0));
        Assert.Equal(100, geometry.ContourLength, 3);
    }

    [Fact]
    public void Open_Should_Not_Affect_Clone()
    {
        using var app = Start();

        var geometry = new StreamGeometry();
        AddLine(geometry, new Point(0, 0), new Point(10, 10));
        var clone = geometry.Clone();

        AddLine(geometry, new Point(20, 20), new Point(30, 30));

        Assert.Equal(new Rect(0, 0, 10, 10), clone.Bounds);
        Assert.Equal(new Rect(0, 0, 30, 30), geometry.Bounds);
    }

    [Fact]
    public void Clone_Should_Keep_Transform()
    {
        using var app = Start();

        var geometry = StreamGeometry.Parse("M10,190 l190,-190");
        geometry.Transform = new TranslateTransform(50, 50);

        var clone = geometry.Clone();

        Assert.NotNull(clone.Transform);
        Assert.Equal(geometry.Transform.Value, clone.Transform.Value);
        Assert.Equal(geometry.Bounds, clone.Bounds);
    }

    private static IDisposable Start()
        => UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
            .With(renderInterface: new PlatformRenderInterface()));

    private static void AddLine(StreamGeometry geometry, Point start, Point end)
    {
        using var context = geometry.Open();
        context.BeginFigure(start, false);
        context.LineTo(end);
        context.EndFigure(false);
    }
}
