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


    }
}
