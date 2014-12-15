// -----------------------------------------------------------------------
// <copyright file="EllipseTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.RenderTests.Shapes
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Controls;
    using Perspex.Media;
    using Perspex.Controls.Shapes;

    [TestClass]
    public class EllipseTests : TestBase
    {
        public EllipseTests()
            : base(@"Shapes\Ellipse")
        {
        }

        [TestMethod]
        public void Circle_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Ellipse
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
