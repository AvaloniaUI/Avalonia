using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class StreamGeometryTests
{
    [Fact]
    public void Changed_Is_Raised_When_The_Drawing_Is_Finished()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var target = new StreamGeometry();
        var changed = 0;

        target.Changed += (_, _) => changed++;

        using (var context = target.Open())
        {
            Assert.Equal(0, changed);

            context.BeginFigure(new Point(0, 0), true);
            context.LineTo(new Point(10, 0));
            context.EndFigure(true);

            Assert.Equal(0, changed);
        }

        Assert.Equal(1, changed);
    }

    [Fact]
    public void Changed_Is_Raised_When_The_Drawing_Is_Finished_And_A_Transform_Is_Set()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var target = new StreamGeometry { Transform = new TranslateTransform(10, 20) };
        var changed = 0;

        target.Changed += (_, _) => changed++;

        using (var context = target.Open())
        {
            Assert.Equal(0, changed);

            context.BeginFigure(new Point(0, 0), true);
            context.LineTo(new Point(10, 0));
            context.EndFigure(true);

            Assert.Equal(0, changed);
        }

        Assert.Equal(1, changed);
    }
}
