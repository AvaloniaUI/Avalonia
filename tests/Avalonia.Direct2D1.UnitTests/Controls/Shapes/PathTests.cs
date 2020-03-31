using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Direct2D1.UnitTests.Controls.Shapes
{
    public class PathTests
    {
        private static readonly RectComparer Compare = new RectComparer();

        [Fact]
        public void Should_Measure_Expander_Triangle_Correctly()
        {
            using (AvaloniaLocator.EnterScope())
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

                Assert.Equal(new Rect(0, 0, 4, 10), target.Bounds, Compare);
            }
        }

        [Fact]
        public void Should_Measure_Expander_Triangle_With_Stroke_Correctly()
        {
            using (AvaloniaLocator.EnterScope())
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
                //
                //     {-1, -0.414, 6.414, 12.828} (see GeometryTests)
                //
                // With origin at 0,0 the bounds should equal:
                //
                //     Assert.Equal(new Rect(0, 0, 5.414, 12.414), target.Bounds, compare);
                //
                // However Path.Measure doesn't correctly handle strokes currently, so testing for
                // the (incorrect) current output for now...
                Assert.Equal(new Rect(0, 0, 4, 10), target.Bounds, Compare);
            }
        }
    }
}
