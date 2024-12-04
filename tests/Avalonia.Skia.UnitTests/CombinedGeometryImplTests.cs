using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class CombinedGeometryImplTests
{
    [Fact]
    public void Combining_Fill_With_Empty_Stroke_Returns_Fill_Bounds()
    {
        var fill = new SKPath();
        fill.LineTo(100, 0);
        fill.LineTo(100, 100);
        fill.LineTo(0, 100);
        fill.Close();

        var stroke = new SKPath();

        var result = new CombinedGeometryImpl(stroke, fill);

        Assert.Equal(new Rect(0, 0, 100, 100), result.Bounds);
    }
}
