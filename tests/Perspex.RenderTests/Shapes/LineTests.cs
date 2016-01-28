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
    public class LineTests : TestBase
    {
        public LineTests()
            : base(@"Shapes\Line")
        {
        }

        [Fact]
        public void Line_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(200, 200)
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void Line_1px_Stroke_Reversed()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StartPoint = new Point(200, 0),
                    EndPoint = new Point(0, 200)
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void Line_1px_Stroke_Vertical()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StartPoint = new Point(100, 200),
                    EndPoint = new Point(100, 0)
                }
            };

            RenderToFile(target);
            CompareImages();
        }
    }
}
