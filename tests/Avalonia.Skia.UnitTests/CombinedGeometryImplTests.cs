using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class CombinedGeometryImplTests
{
    [Fact]
    public void Combining_Fill_With_Empty_Stroke_Returns_Fill_Bounds()
    {
        using var builder = new SKPathBuilder();
        builder.LineTo(100, 0);
        builder.LineTo(100, 100);
        builder.LineTo(0, 100);
        builder.Close();
        var fill = builder.Detach();

        var stroke = new SKPath();

        var result = new CombinedGeometryImpl(stroke, fill);

        Assert.Equal(new Rect(0, 0, 100, 100), result.Bounds);
    }
}
