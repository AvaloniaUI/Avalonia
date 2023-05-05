using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Shapes
#endif
{
    public class EllipseTests : TestBase
    {
        public EllipseTests()
            : base(@"Shapes\Ellipse")
        {
        }

        [Fact]
        public async Task Circle_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Ellipse
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_Circle_Aliased()
        {
            var target = new Border
            {
                Background = Brushes.White,
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Ellipse
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 3.5,
                }
            };

            RenderOptions.SetEdgeMode(target, EdgeMode.Aliased);

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_Circle_Antialiased()
        {
            var target = new Border
            {
                Background = Brushes.White,
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Ellipse
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 3.5,
                }
            };

            RenderOptions.SetEdgeMode(target, EdgeMode.Antialias);

            await RenderToFile(target);
            CompareImages();
        }
    }
}
