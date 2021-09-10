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

        [Fact]
        public async Task FillRule_EvenOdd()
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
                        FillRule = FillRule.EvenOdd,
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
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task FillRule_NonZero()
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
                        FillRule = FillRule.NonZero,
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
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task FillRule_EvenOdd_Stroke()
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
                        FillRule = FillRule.EvenOdd,
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

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task FillRule_NonZero_Stroke()
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
                        FillRule = FillRule.NonZero,
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

            await RenderToFile(target);
            CompareImages();
        }
    }
}
