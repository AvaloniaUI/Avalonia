using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class CombinedGeometryImplTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Combining_Fill_With_Empty_Stroke_Returns_Fill_Bounds(bool curved, bool nullStroke)
    {
        using var fill = new SKPath();
        fill.MoveTo(0, 0);
        if (curved)
            fill.QuadTo(200, 0, 0, 100);
        else
        {
            fill.LineTo(100, 0);
            fill.LineTo(100, 100);
            fill.LineTo(0, 100);
        }
        fill.Close();

        using var stroke = nullStroke ? null : new SKPath();

        var result = new CombinedGeometryImpl(stroke, fill);

        HitTesting.AssertGeometryCanBeShared(result, new Point(50, 50));
        Assert.Equal(new Rect(0, 0, 100, 100), result.Bounds);
    }
}
