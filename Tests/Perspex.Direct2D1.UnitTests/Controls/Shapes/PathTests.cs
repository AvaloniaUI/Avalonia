// -----------------------------------------------------------------------
// <copyright file="GeometryTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.UnitTests.Controls.Shapes
{
    using Perspex.Controls.Shapes;
    using Perspex.Layout;
    using Perspex.Media;
    using Splat;
    using Xunit;

    public class PathTests
    {
        private static readonly RectComparer compare = new RectComparer();

        [Fact]
        void Should_Measure_Expander_Triangle_Correctly()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                Direct2D1Platform.Initialize();

                var target = new Path
                {
                    Data = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z"),
                    StrokeThickness = 0,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    UseLayoutRounding = false,
                };

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                Assert.Equal(new Rect(0, 0, 4, 10), target.Bounds, compare);
            }
        }

        [Fact]
        void Should_Measure_Expander_Triangle_With_Stroke_Correctly()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                Direct2D1Platform.Initialize();

                var target = new Path
                {
                    Data = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z"),
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    UseLayoutRounding = false,
                };

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                // Measured geometry with stroke of 2px is:
                //     {-1, -0.414, 6.414, 12.828} (see GeometryTests)
                // With origin at 0,0 the bounds equal:
                Assert.Equal(new Rect(0, 0, 5.414, 12.414), target.Bounds, compare);
            }
        }
    }
}

//{-0.5,0.79289323091507,5.207106590271,10.4142133593559}
//{-1,-0.414213567972183,6.41421365737915,12.8284267485142}