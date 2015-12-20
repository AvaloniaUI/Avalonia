// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Xunit;

namespace Perspex.Layout.UnitTests
{
    public class MeasureTests
    {
        [Fact]
        public void Negative_Margin_Larger_Than_Constraint_Should_Request_Width_0()
        {
            Control target;

            var outer = new Decorator
            {
                Width = 100,
                Height = 100,
                Child = target = new Control
                {
                    Margin = new Thickness(-100, 0, 0, 0),
                }
            };

            outer.Measure(Size.Infinity);

            Assert.Equal(0, target.DesiredSize.Width);
        }

        [Fact]
        public void Negative_Margin_Larger_Than_Constraint_Should_Request_Height_0()
        {
            Control target;

            var outer = new Decorator
            {
                Width = 100,
                Height = 100,
                Child = target = new Control
                {
                    Margin = new Thickness(0, -100, 0, 0),
                }
            };

            outer.Measure(Size.Infinity);

            Assert.Equal(0, target.DesiredSize.Height);
        }
    }
}
