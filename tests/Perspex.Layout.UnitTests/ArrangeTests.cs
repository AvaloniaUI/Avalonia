// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Xunit;

namespace Perspex.Layout.UnitTests
{
    public class ArrangeTests
    {
        [Fact]
        public void Arrange_With_IsMeasureValid_False_Calls_Measure()
        {
            var target = new TestControl();

            Assert.False(target.IsMeasureValid);
            target.Arrange(new Rect(0, 0, 120, 120));
            Assert.True(target.IsMeasureValid);
            Assert.Equal(new Size(120, 120), target.MeasureConstraint);
        }

        private class TestControl : Border
        {
            public Size MeasureConstraint { get; private set; }

            protected override Size MeasureOverride(Size constraint)
            {
                MeasureConstraint = constraint;
                return base.MeasureOverride(constraint);
            }
        }
    }
}
