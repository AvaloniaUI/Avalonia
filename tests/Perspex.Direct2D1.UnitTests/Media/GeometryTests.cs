// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Media;
using Splat;
using Xunit;

namespace Perspex.Direct2D1.UnitTests.Media
{
    public class GeometryTests
    {
        private static readonly RectComparer Compare = new RectComparer();

        [Fact]
        public void Should_Measure_Expander_Triangle_Correctly()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                Direct2D1Platform.Initialize();

                var target = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z");

                Assert.Equal(new Rect(0, 2, 4, 8), target.Bounds, Compare);
            }
        }

        [Fact]
        public void Should_Measure_Expander_Triangle_With_Stroke_Correctly()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                Direct2D1Platform.Initialize();

                var target = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z");

                Assert.Equal(new Rect(-1, -0.414, 6.414, 12.828), target.GetRenderBounds(2), Compare);
            }
        }
    }
}
