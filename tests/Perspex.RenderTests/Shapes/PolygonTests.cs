// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Controls.Shapes;
using Perspex.Media;
using Xunit;

#if PERSPEX_CAIRO
namespace Perspex.Cairo.RenderTests.Shapes
#elif PERSPEX_SKIA
namespace Perspex.Skia.RenderTests
#else
namespace Perspex.Direct2D1.RenderTests.Shapes
#endif
{
    public class PolygonTests : TestBase
    {
        public PolygonTests()
            : base(@"Shapes\Polygon")
        {
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "Caused by cairo bug")]
#else
        [Fact]
#endif
        public void Polygon_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Polygon
                {
                    Stroke = Brushes.DarkBlue,
                    Stretch = Stretch.Uniform,
                    Fill = Brushes.Violet,
                    Points = new [] { new Point(5, 0), new Point(8, 8), new Point(0, 3), new Point(10, 3), new Point(2, 8) },
                    StrokeThickness = 1
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "Caused by cairo bug")]
#else
        [Fact]
#endif
        public void Polygon_NonUniformFill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 400,
                Height = 200,
                Child = new Polygon
                {
                    Stroke = Brushes.DarkBlue,
                    Stretch = Stretch.Fill,
                    Fill = Brushes.Violet,
                    Points = new[] { new Point(5, 0), new Point(8, 8), new Point(0, 3), new Point(10, 3), new Point(2, 8) },
                    StrokeThickness = 5,
                }
            };

            RenderToFile(target);
            CompareImages();
        }
    }
}
