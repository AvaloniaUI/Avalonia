// -----------------------------------------------------------------------
// <copyright file="GeometryTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.UnitTests.Media
{
    using Perspex.Media;
    using Splat;
    using Xunit;

    public class GeometryTests
    {
        private static readonly RectComparer compare = new RectComparer();

        [Fact]
        void Should_Measure_Expander_Triangle_Correctly()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                Direct2D1Platform.Initialize();

                var target = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z");

                Assert.Equal(new Rect(0, 2, 4, 8), target.Bounds, compare);
            }
        }

        [Fact]
        void Should_Measure_Expander_Triangle_With_Stroke_Correctly()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                Direct2D1Platform.Initialize();

                var target = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z");

                Assert.Equal(new Rect(-1, -0.414, 6.414, 12.828), target.GetRenderBounds(2), compare);
            }
        }
    }
}
