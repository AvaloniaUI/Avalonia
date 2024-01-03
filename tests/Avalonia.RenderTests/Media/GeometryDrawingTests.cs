using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class GeometryDrawingTests : TestBase
    {
        public GeometryDrawingTests()
            : base(@"Media\GeometryDrawing")
        {
        }

        private static GeometryDrawing CreateGeometryDrawing()
        {
            GeometryDrawing geometryDrawing = new GeometryDrawing();
            EllipseGeometry ellipse = new EllipseGeometry();
            ellipse.RadiusX = 100;
            ellipse.RadiusY = 100;
            geometryDrawing.Geometry = ellipse;
            return geometryDrawing;
        }

        [Fact]
        public void DrawingGeometry_WithPen()
        {
            GeometryDrawing geometryDrawing = CreateGeometryDrawing();
            geometryDrawing.Pen = new Pen(new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)), 10);

            Assert.Equal(210, geometryDrawing.GetBounds().Height);
            Assert.Equal(210, geometryDrawing.GetBounds().Width);

        }

        [Fact]
        public void DrawingGeometry_WithoutPen()
        {
            GeometryDrawing geometryDrawing = CreateGeometryDrawing();

            Assert.Equal(200, geometryDrawing.GetBounds().Height);
            Assert.Equal(200, geometryDrawing.GetBounds().Width);
        }

        [Theory]
        [InlineData("l", "F1 M50,50z l -5,-5")]
        [InlineData("h", "F1 M50,50z h 10")]
        [InlineData("v", "F1 M50,50z v 10")]
        [InlineData("m", "M50,50z l -5,-5")]
        public async void DrawingGeometry_RelativeLine_Commands(string name, string command)
        {
            var target = new Avalonia.Controls.Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Avalonia.Controls.Image()
                {
                    Width = 200,
                    Height = 200,
                    Source = new DrawingImage()
                    {
                        Drawing = new GeometryDrawing()
                        {
                            Geometry = StreamGeometry.Parse(command),
                            Pen = new Pen(Brushes.Black, 2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round)
                        }
                    },

                }
            };
            var testName = $"Relative_Line_{name}";
            await RenderToFile(target, testName);
            CompareImages(testName);
        }
    }
}
