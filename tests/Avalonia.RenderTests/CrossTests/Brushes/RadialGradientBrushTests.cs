using Avalonia.Media;
using CrossUI;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;
#elif AVALONIA_D2D
namespace Avalonia.Direct2D1.RenderTests;
#else
namespace Avalonia.RenderTests.WpfCompare;
#endif


public class CrossRadialGradientBrushTests : CrossTestBase
{
    public CrossRadialGradientBrushTests() : base("Media/RadialGradientBrush")
    {
    }

    [CrossFact]
    public void Transform_Should_Work_As_Expected()
    {
        RenderAndCompare(
            new CrossControl()
            {
                Children =
                {
                    new CrossFuncControl(ctx =>
                    {
                        var geo = new CrossEllipseGeometry(new Rect(3.430200000000003, 29.019099999999998, 42.7692,
                            19.6732));
                        ctx.DrawGeometry(new CrossSolidColorBrush(Colors.Magenta), null, geo);
                        ctx.DrawGeometry(
                            new CrossRadialGradientBrush()
                            {
                                RadiusX = 12.289,
                                RadiusY = 12.289,
                                GradientOrigin = new Point(15.116, 63.965),
                                Center = new Point(15.116, 63.965),
                                MappingMode = BrushMappingMode.Absolute,
                                SpreadMethod = GradientSpreadMethod.Pad,
                                GradientStops =
                                {
                                    new GradientStop(Colors.Black, 0), new GradientStop(Colors.Transparent, 1)
                                },
                                Transform = new Matrix(1.664, 0,
                                    0, 0.75621371,
                                    -0.06567275, -10.272)
                            }, null, geo);
                    })
                    {
                        Width = 48,
                        Height = 48,
                        RenderTransform = Matrix.CreateScale(4, 4)
                    }
                },
                Width = 256,
                Height = 256,
                Background = new CrossSolidColorBrush(Colors.White)
            });

    }
}
