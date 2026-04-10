using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class DrawingImageTests : ScopedTestBase
    {
        [Fact]
        public void Drawing_Mutation_In_Visual_Tree_Raises_DrawingImage_Invalidated()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var geometryDrawing = new GeometryDrawing
                {
                    Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                    Brush = Brushes.Blue
                };

                var drawingImage = new DrawingImage(geometryDrawing);

                var image = new Image { Source = drawingImage };

                var window = new Window { Content = image };
                window.Show();

                var raised = false;
                drawingImage.Invalidated += (_, _) => raised = true;

                geometryDrawing.Brush = Brushes.Red;

                Assert.True(raised);
            }
        }
    }
}
