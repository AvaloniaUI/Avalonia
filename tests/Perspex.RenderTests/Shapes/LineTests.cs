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
        public void Circle_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                }
            };

            RenderToFile(target);
            CompareImages();
        }
    }
}
