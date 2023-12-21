using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else

using Avalonia.Direct2D1.RenderTests;

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

        [Fact]
        public async void DrawingGeometry_RelativeLine()
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
                            Geometry = StreamGeometry.Parse("F1 M50,50z l -5,-5"),
                            Pen = new Pen(Brushes.Black, 2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round)
                        }
                    },

                }
            };
            await RenderToFile(target);
            CompareImages();
        }
    }
}
