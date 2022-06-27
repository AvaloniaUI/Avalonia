using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Xunit;
using Xunit.Sdk;

namespace Avalonia.Base.UnitTests.Layout
{
    public class LayoutableTests_LayoutRounding
    {
        [Theory]
        [InlineData(100, 100)]
        [InlineData(101, 101.33333333333333)]
        [InlineData(103, 103.33333333333333)]
        public void Measure_Adjusts_DesiredSize_Upwards_When_Constraint_Allows(double desiredSize, double expectedSize)
        {
            var target = new TestLayoutable(new Size(desiredSize, desiredSize));
            var root = CreateRoot(1.5, target);

            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal(new Size(expectedSize, expectedSize), target.DesiredSize);
        }

        [Fact]
        public void Measure_Constrains_Adjusted_DesiredSize_To_Constraint()
        {
            var target = new TestLayoutable(new Size(101, 101));
            var root = CreateRoot(1.5, target, constraint: new Size(101, 101));

            root.LayoutManager.ExecuteInitialLayoutPass();

            // Desired width/height with layout rounding is 101.3333 but constraint is 101,101 so
            // layout rounding should be ignored.
            Assert.Equal(new Size(101, 101), target.DesiredSize);
        }

        [Fact]
        public void Measure_Adjusts_DesiredSize_Upwards_When_Margin_Present()
        {
            var target = new TestLayoutable(new Size(101, 101), margin: 1);
            var root = CreateRoot(1.5, target);

            root.LayoutManager.ExecuteInitialLayoutPass();

            // - 1 pixel margin is rounded up to 1.3333; for both sides it is 2.6666
            // - Size of 101 gets rounded up to 101.3333
            // - Final size = 101.3333 + 2.6666 = 104
            AssertEqual(new Size(104, 104), target.DesiredSize);
        }

        [Fact]
        public void Arrange_Adjusts_Bounds_Upwards_With_Margin()
        {
            var target = new TestLayoutable(new Size(101, 101), margin: 1);
            var root = CreateRoot(1.5, target);

            root.LayoutManager.ExecuteInitialLayoutPass();

            // - 1 pixel margin is rounded up to 1.3333
            // - Size of 101 gets rounded up to 101.3333
            AssertEqual(new Point(1.3333333333333333, 1.3333333333333333), target.Bounds.Position);
            AssertEqual(new Size(101.33333333333333, 101.33333333333333), target.Bounds.Size);
        }

        [Theory]
        [InlineData(16, 6, 5.333333333333333)]
        [InlineData(18, 10, 4)]
        public void Arranges_Center_Alignment_Correctly_With_Fractional_Scaling(
            double containerWidth,
            double childWidth,
            double expectedX)
        {
            Border target;
            var root = new TestRoot
            {
                LayoutScaling = 1.5,
                UseLayoutRounding = true,
                Child = new Decorator
                {
                    Width = containerWidth,
                    Height = 100,
                    Child = target = new Border
                    {
                        Width = childWidth,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    }
                }
            };

            root.Measure(new Size(100, 100));
            root.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(expectedX, 0, childWidth, 100), target.Bounds);
        }

        private static TestRoot CreateRoot(
            double scaling,
            Control child,
            Size? constraint = null)
        {
            return new TestRoot
            {
                LayoutScaling = scaling,
                UseLayoutRounding = true,
                Child = child,
                ClientSize = constraint ?? new Size(1000, 1000),
            };
        }

        private static void AssertEqual(Point expected, Point actual)
        {
            if (!expected.NearlyEquals(actual))
            {
                throw new EqualException(expected, actual);
            }
        }

        private static void AssertEqual(Size expected, Size actual)
        {
            if (!expected.NearlyEquals(actual))
            {
                throw new EqualException(expected, actual);
            }
        }

        private class TestLayoutable : Control
        {
            private Size _desiredSize;

            public TestLayoutable(Size desiredSize, double margin = 0)
            {
                _desiredSize = desiredSize;
                Margin = new Thickness(margin);
            }

            protected override Size MeasureOverride(Size availableSize) => _desiredSize;
        }
    }
}
