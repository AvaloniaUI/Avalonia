// -----------------------------------------------------------------------
// <copyright file="RectangleTests.cs" company="Steven Kirk">
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
    public class RectangleTests : TestBase
    {
        public RectangleTests()
            : base(@"Shapes\Rectangle")
        {
        }

        [TestMethod]
        public void Rectangle_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Rectangle_2px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Rectangle_Stroke_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.Red,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }
    }
}
