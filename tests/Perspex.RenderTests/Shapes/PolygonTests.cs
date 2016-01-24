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

        [Fact]
        public void Polygon_1px_Stroke()
        {
            var polygonPoints = new Point[] { new Point(5, 0), new Point(8, 8), new Point(0, 3), new Point(10, 3), new Point(2, 8) };
            for (int i = 0; i < polygonPoints.Length; i++)
            {
                polygonPoints[i] = polygonPoints[i] * 15;
            }

            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 150,
                Child = new Polygon
                {
                    Stroke = Brushes.DarkBlue,
                    Fill = Brushes.Violet,
                    Points = polygonPoints,
                    StrokeThickness = 1
                }
            };

            RenderToFile(target);
            CompareImages();
        }
    }
}
