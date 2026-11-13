using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class RadialGradientBrushTests : ScopedTestBase
{
    [Fact]
    public void ToImmutable_Copies_RadiusX_And_RadiusY()
    {
        var brush = new RadialGradientBrush
        {
            RadiusX = new RelativeScalar(0.25, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.75, RelativeUnit.Relative),
        };

        var immutable = (ImmutableRadialGradientBrush)brush.ToImmutable();

        Assert.Equal(brush.RadiusX, immutable.RadiusX);
        Assert.Equal(brush.RadiusY, immutable.RadiusY);
    }
}
