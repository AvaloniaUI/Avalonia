// -----------------------------------------------------------------------
// <copyright file="RectangleTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.RenderTests.Shapes
{
    using Perspex.Controls;
    using Perspex.Media;
    using Perspex.Controls.Shapes;
    using Xunit;

    public class RectangleTests : TestBase
    {
        public RectangleTests()
            : base(@"Shapes\Rectangle")
        {
        }

        [Fact]
        public void Rectangle_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void Rectangle_2px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void Rectangle_Stroke_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
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
