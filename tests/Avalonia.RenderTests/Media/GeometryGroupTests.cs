using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class GeometryGroupTests : TestBase
    {
        public GeometryGroupTests()
            : base(@"Media\GeometryGroup")
        {
        }

        [Theory]
        [InlineData(FillRule.EvenOdd)]
        [InlineData(FillRule.NonZero)]
        public async Task FillRule_Stroke(FillRule fillRule)
        {
            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = Brushes.White,
                Child = new Path
                {
                    Data = new GeometryGroup
                    {
                        FillRule = fillRule,
                        Children =
                        {
                            new RectangleGeometry(new Rect(25, 25, 100, 100)),
                            new EllipseGeometry
                            {
                                Center = new Point(125, 125),
                                RadiusX = 50,
                                RadiusY = 50,
                            },
                        }
                    },
                    Fill = Brushes.Blue,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                }
            };

            var testName = $"{nameof(FillRule_Stroke)}_{fillRule}";
            await RenderToFile(target, testName);
            CompareImages(testName);
        }

        [Fact]
        public async Task Child_Transform()
        {
            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = Brushes.White,
                Child = new Path
                {
                    Data = new GeometryGroup
                    {
                        Children =
                        {
                            new RectangleGeometry(new Rect(25, 25, 100, 100))
                            {
                                Transform = new RotateTransform(45, 75, 75)
                            },
                            new EllipseGeometry
                            {
                                Center = new Point(125, 125),
                                RadiusX = 50,
                                RadiusY = 50,
                            },
                        }
                    },
                    Fill = Brushes.Blue,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
