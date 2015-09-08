





namespace Perspex.Controls.UnitTests.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using Perspex.Controls.Presenters;
    using Perspex.Layout;
    using Xunit;

    public class ScrollContentPresenterTests
    {
        [Fact]
        public void Content_Can_Be_Left_Aligned()
        {
            Border content;
            var target = new ScrollContentPresenter
            {
                Content = content = new Border
                {
                    Padding = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Left
                },
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 0, 16, 100), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_Stretched()
        {
            Border content;
            var target = new ScrollContentPresenter
            {
                Content = content = new Border
                {
                    Padding = new Thickness(8),
                },
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 0, 100, 100), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_Right_Aligned()
        {
            Border content;
            var target = new ScrollContentPresenter
            {
                Content = content = new Border
                {
                    Padding = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Right
                },
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(84, 0, 16, 100), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_Bottom_Aligned()
        {
            Border content;
            var target = new ScrollContentPresenter
            {
                Content = content = new Border
                {
                    Padding = new Thickness(8),
                    VerticalAlignment = VerticalAlignment.Bottom,
                },
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 84, 100, 16), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_TopRight_Aligned()
        {
            Border content;
            var target = new ScrollContentPresenter
            {
                Content = content = new Border
                {
                    Padding = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                },
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(84, 0, 16, 16), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_Larger_Than_Viewport()
        {
            TestControl content;
            var target = new ScrollContentPresenter
            {
                Content = content = new TestControl(),
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 0, 150, 150), content.Bounds);
        }

        [Fact]
        public void Content_Can_Be_Offset()
        {
            Border content;
            var target = new ScrollContentPresenter
            {
                Content = content = new Border
                {
                    Width = 150,
                    Height = 150,
                },
                Offset = new Vector(25, 25),
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(-25, -25, 150, 150), content.Bounds);
        }

        [Fact]
        public void Arrange_Should_Set_Viewport_And_Extent_In_That_Order()
        {
            var target = new ScrollContentPresenter
            {
                Content = new Border { Width = 40, Height = 50 }
            };

            var set = new List<string>();

            target.Measure(new Size(100, 100));

            target.GetObservable(ScrollViewer.ViewportProperty).Skip(1).Subscribe(_ => set.Add("Viewport"));
            target.GetObservable(ScrollViewer.ExtentProperty).Skip(1).Subscribe(_ => set.Add("Extent"));

            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new[] { "Viewport", "Extent" }, set);
        }

        [Fact]
        public void Setting_Offset_Should_Invalidate_Arrange()
        {
            var target = new ScrollContentPresenter
            {
                Content = new Border { Width = 40, Height = 50 }
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));
            target.Offset = new Vector(10, 100);

            Assert.True(target.IsMeasureValid);
            Assert.False(target.IsArrangeValid);
        }

        private class TestControl : Control
        {
            protected override Size MeasureOverride(Size availableSize)
            {
                return new Size(150, 150);
            }
        }
    }
}