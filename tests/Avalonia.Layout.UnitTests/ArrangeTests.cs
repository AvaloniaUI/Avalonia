// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Xunit;

namespace Avalonia.Layout.UnitTests
{
    public class ArrangeTests
    {
        [Fact]
        public void Margin_Should_Be_Subtracted_From_Arrange_FinalSize()
        {
            var target = new TestControl
            {
                Width = 100,
                Height = 100,
                Margin = new Thickness(8),
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 100), target.ArrangeFinalSize);
        }

        [Fact]
        public void ArrangeOverride_Receives_Desired_Size_When_Centered()
        {
            var target = new TestControl
            {
                MeasureResult = new Size(100, 100),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8),
            };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.Arrange(new Rect(0, 0, 200, 200));

            Assert.Equal(new Size(100, 100), target.ArrangeFinalSize);
        }

        [Fact]
        public void ArrangeOverride_Receives_Available_Size_Minus_Margin_When_Stretched()
        {
            var target = new TestControl
            {
                MeasureResult = new Size(100, 100),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(8),
            };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.Arrange(new Rect(0, 0, 200, 200));

            Assert.Equal(new Size(184, 184), target.ArrangeFinalSize);
        }

        [Fact]
        public void ArrangeOverride_Receives_Requested_Size_When_Arranged_To_DesiredSize()
        {
            var target = new TestControl
            {
                MeasureResult = new Size(100, 100),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8),
            };

            target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 100), target.ArrangeFinalSize);
        }

        [Fact]
        public void Arrange_With_IsMeasureValid_False_Calls_Measure()
        {
            var target = new TestControl();

            Assert.False(target.IsMeasureValid);
            target.Arrange(new Rect(0, 0, 120, 120));
            Assert.True(target.IsMeasureValid);
            Assert.Equal(new Size(120, 120), target.MeasureConstraint);
        }

        [Fact]
        public void Arrange_With_IsMeasureValid_False_Calls_Measure_With_Previous_Size_If_Available()
        {
            var target = new TestControl();

            Assert.False(target.IsMeasureValid);
            target.Arrange(new Rect(0, 0, 120, 120));
            target.InvalidateMeasure();
            target.Arrange(new Rect(0, 0, 100, 100));
            Assert.True(target.IsMeasureValid);
            Assert.Equal(new Size(120, 120), target.MeasureConstraint);
        }

        private class TestControl : Decorator
        {
            public Size MeasureConstraint { get; private set; }

            public Size MeasureResult { get; set; }

            public Size ArrangeFinalSize { get; private set; }

            protected override Size MeasureOverride(Size constraint)
            {
                MeasureConstraint = constraint;
                return MeasureResult;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                ArrangeFinalSize = finalSize;
                return base.ArrangeOverride(finalSize);
            }
        }
    }
}
