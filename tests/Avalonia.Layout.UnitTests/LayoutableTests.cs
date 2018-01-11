using System;
using Avalonia.Controls;
using Moq;
using Xunit;

namespace Avalonia.Layout.UnitTests
{
    public class LayoutableTests
    {
        [Theory]
        [InlineData(0, 0, 0, 0, 100, 100)]
        [InlineData(10, 0, 0, 0, 90, 100)]
        [InlineData(10, 0, 5, 0, 85, 100)]
        [InlineData(0, 10, 0, 0, 100, 90)]
        [InlineData(0, 10, 0, 5, 100, 85)]
        [InlineData(4, 4, 6, 7, 90, 89)]
        public void Margin_Is_Applied_To_MeasureOverride_Size(
            double l,
            double t,
            double r,
            double b,
            double expectedWidth,
            double expectedHeight)
        {
            var target = new TestLayoutable
            {
                Margin = new Thickness(l, t, r, b),
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(expectedWidth, expectedHeight), target.MeasureSize);
        }

        [Theory]
        [InlineData(HorizontalAlignment.Stretch, 100)]
        [InlineData(HorizontalAlignment.Left, 10)]
        [InlineData(HorizontalAlignment.Center, 10)]
        [InlineData(HorizontalAlignment.Right, 10)]
        public void HorizontalAlignment_Is_Applied_To_ArrangeOverride_Size(
            HorizontalAlignment h,
            double expectedWidth)
        {
            var target = new TestLayoutable
            {
                HorizontalAlignment = h,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(expectedWidth, 100), target.ArrangeSize);
        }

        [Theory]
        [InlineData(VerticalAlignment.Stretch, 100)]
        [InlineData(VerticalAlignment.Top, 10)]
        [InlineData(VerticalAlignment.Center, 10)]
        [InlineData(VerticalAlignment.Bottom, 10)]
        public void VerticalAlignment_Is_Applied_To_ArrangeOverride_Size(
            VerticalAlignment v,
            double expectedHeight)
        {
            var target = new TestLayoutable
            {
                VerticalAlignment = v,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(100, expectedHeight), target.ArrangeSize);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 100, 100)]
        [InlineData(10, 0, 0, 0, 90, 100)]
        [InlineData(10, 0, 5, 0, 85, 100)]
        [InlineData(0, 10, 0, 0, 100, 90)]
        [InlineData(0, 10, 0, 5, 100, 85)]
        [InlineData(4, 4, 6, 7, 90, 89)]
        public void Margin_Is_Applied_To_ArrangeOverride_Size(
            double l,
            double t,
            double r,
            double b,
            double expectedWidth,
            double expectedHeight)
        {
            var target = new TestLayoutable
            {
                Margin = new Thickness(l, t, r, b),
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(expectedWidth, expectedHeight), target.ArrangeSize);
        }

        [Fact]
        public void Only_Calls_LayoutManager_InvalidateMeasure_Once()
        {
            var target = new Mock<ILayoutManager>();

            using (Start(target.Object))
            {
                var control = new Decorator();
                var root = new LayoutTestRoot { Child = control };

                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));
                target.ResetCalls();

                control.InvalidateMeasure();
                control.InvalidateMeasure();

                target.Verify(x => x.InvalidateMeasure(control), Times.Once());
            }
        }

        [Fact]
        public void Only_Calls_LayoutManager_InvalidateArrange_Once()
        {
            var target = new Mock<ILayoutManager>();

            using (Start(target.Object))
            {
                var control = new Decorator();
                var root = new LayoutTestRoot { Child = control };

                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));
                target.ResetCalls();

                control.InvalidateArrange();
                control.InvalidateArrange();

                target.Verify(x => x.InvalidateArrange(control), Times.Once());
            }
        }

        [Fact]
        public void Attaching_Control_To_Tree_Invalidates_Parent_Measure()
        {
            var target = new Mock<ILayoutManager>();

            using (Start(target.Object))
            {
                var control = new Decorator();
                var root = new LayoutTestRoot { Child = control };

                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));
                Assert.True(control.IsMeasureValid);

                root.Child = null;
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                Assert.False(control.IsMeasureValid);
                Assert.True(root.IsMeasureValid);

                target.ResetCalls();

                root.Child = control;

                Assert.False(root.IsMeasureValid);
                Assert.False(control.IsMeasureValid);
                target.Verify(x => x.InvalidateMeasure(root), Times.Once());
            }
        }

        private IDisposable Start(ILayoutManager layoutManager)
        {
            var result = AvaloniaLocator.EnterScope();
            AvaloniaLocator.CurrentMutable.Bind<ILayoutManager>().ToConstant(layoutManager);
            return result;
        }

        private class TestLayoutable : Layoutable
        {
            public Size ArrangeSize { get; private set; }
            public Size MeasureResult { get; set; } = new Size(10, 10);
            public Size MeasureSize { get; private set; }

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureSize = availableSize;
                return MeasureResult;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                ArrangeSize = finalSize;
                return base.ArrangeOverride(finalSize);
            }
        }
    }
}
