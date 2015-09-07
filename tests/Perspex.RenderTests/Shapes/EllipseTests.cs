// -----------------------------------------------------------------------
// <copyright file="EllipseTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

#if PERSPEX_CAIRO
namespace Perspex.Cairo.RenderTests.Shapes
#else
namespace Perspex.Direct2D1.RenderTests.Shapes
#endif
{
    using Perspex.Controls;
    using Perspex.Controls.Shapes;
    using Perspex.Media;
    using Xunit;

    public class EllipseTests : TestBase
    {
        public EllipseTests()
            : base(@"Shapes\Ellipse")
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
                Child = new Ellipse
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }
    }
}
