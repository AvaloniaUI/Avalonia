// -----------------------------------------------------------------------
// <copyright file="BorderTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.RenderTests.Controls
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Controls;
    using Perspex.Media;

    [TestClass]
    public class BorderTests : TestBase
    {
        public BorderTests()
            : base(@"Controls\Border")
        {
        }

        [TestMethod]
        public void Border_1px_Border()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 1,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }
    }
}
