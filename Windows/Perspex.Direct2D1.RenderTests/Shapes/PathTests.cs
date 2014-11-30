// -----------------------------------------------------------------------
// <copyright file="PathTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.RenderTests.Shapes
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Controls;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Controls.Shapes;

    [TestClass]
    public class PathTests : TestBase
    {
        public PathTests()
            : base(@"Shapes\Path")
        {
        }

        [TestMethod]
        public void Path_100px_Triangle_Centered()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Content = new Path
                {
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M 0,100 L 100,100 50,0 Z"),
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }
    }
}
