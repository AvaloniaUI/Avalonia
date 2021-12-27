using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class CombinedGeometryTests : TestBase
    {
        public CombinedGeometryTests()
            : base(@"Media\CombinedGeometry")
        {
        }

        [Theory]
        [InlineData(Avalonia.Media.GeometryCombineMode.Union)]
        [InlineData(Avalonia.Media.GeometryCombineMode.Intersect)]
        [InlineData(Avalonia.Media.GeometryCombineMode.Xor)]
        [InlineData(Avalonia.Media.GeometryCombineMode.Exclude)]
        public async Task GeometryCombineMode(GeometryCombineMode mode)
        {
            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = Brushes.White,
                Child = new Path
                {
                    Data = new CombinedGeometry
                    {
                        GeometryCombineMode = mode,
                        Geometry1 = new RectangleGeometry(new Rect(25, 25, 100, 100)),
                        Geometry2 = new EllipseGeometry
                        {
                            Center = new Point(125, 125),
                            RadiusX = 50,
                            RadiusY = 50,
                        }
                    },
                    Fill = Brushes.Blue,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                }
            };

            var testName = $"{nameof(GeometryCombineMode)}_{mode}";
            await RenderToFile(target, testName);
            CompareImages(testName);
        }

        [Fact]
        public async Task Geometry1_Transform()
        {
            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = Brushes.White,
                Child = new Path
                {
                    Data = new CombinedGeometry
                    {
                        Geometry1 = new RectangleGeometry(new Rect(25, 25, 100, 100))
                        {
                            Transform = new RotateTransform(45, 75, 75)
                        },
                        Geometry2 = new EllipseGeometry
                        {
                            Center = new Point(125, 125),
                            RadiusX = 50,
                            RadiusY = 50,
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
