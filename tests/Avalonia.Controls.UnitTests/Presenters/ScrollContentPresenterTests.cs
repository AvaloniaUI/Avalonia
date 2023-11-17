using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class ScrollContentPresenterTests
    {
        [Theory]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 10, 80, 80)]
        [InlineData(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 10, 16, 80)]
        [InlineData(HorizontalAlignment.Right, VerticalAlignment.Stretch, 74, 10, 16, 80)]
        [InlineData(HorizontalAlignment.Center, VerticalAlignment.Stretch, 42, 10, 16, 80)]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 10, 80, 16)]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 74, 80, 16)]
        [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 42, 80, 16)]
        public void Alignment_And_Padding_Are_Applied_To_Child_Bounds(
            HorizontalAlignment h,
            VerticalAlignment v,
            double expectedX,
            double expectedY,
            double expectedWidth,
            double expectedHeight)
        {
            Border content;
            var target = new ScrollContentPresenter
            {
                Padding = new Thickness(10),
                Content = content = new Border
                {
                    MinWidth = 16,
                    MinHeight = 16,
                    HorizontalAlignment = h,
                    VerticalAlignment = v,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), content.Bounds);
        }

        [Fact]
        public void DesiredSize_Is_Content_Size_When_Smaller_Than_AvailableSize()
        {
            var target = new ScrollContentPresenter
            {
                Padding = new Thickness(10),
                Content = new Border
                {
                    MinWidth = 16,
                    MinHeight = 16,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(16, 16), target.DesiredSize);
        }

        [Fact]
        public void DesiredSize_Is_AvailableSize_When_Content_Larger_Than_AvailableSize()
        {
            var target = new ScrollContentPresenter
            {
                Padding = new Thickness(10),
                Content = new Border
                {
                    MinWidth = 160,
                    MinHeight = 160,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(100, 100), target.DesiredSize);
        }

        [Fact]
        public void Content_Can_Be_Larger_Than_Viewport()
        {
            TestControl content;
            var target = new ScrollContentPresenter
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                Content = content = new TestControl(),
            };

            target.UpdateChild();
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
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                Content = content = new Border
                {
                    Width = 150,
                    Height = 150,
                },
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            target.Offset = new Vector(25, 25);

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(-25, -25, 150, 150), content.Bounds);
        }

        [Fact]
        public void Measure_Should_Pass_Bounded_X_If_CannotScrollHorizontally()
        {
            var child = new TestControl();
            var target = new ScrollContentPresenter
            {
                CanVerticallyScroll = true,
                Content = child,
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(100, double.PositiveInfinity), child.AvailableSize);
        }

        [Fact]
        public void Measure_Should_Pass_Unbounded_X_If_CanScrollHorizontally()
        {
            var child = new TestControl();
            var target = new ScrollContentPresenter
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                Content = child,
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));

            Assert.Equal(Size.Infinity, child.AvailableSize);
        }

        [Fact]
        public void Arrange_Should_Set_Viewport_And_Extent_In_That_Order()
        {
            var target = new ScrollContentPresenter
            {
                Content = new Border { Width = 40, Height = 50 }
            };

            var set = new List<string>();

            target.UpdateChild();
            target.Measure(new Size(100, 100));

            target.GetObservable(ScrollViewer.ViewportProperty).Skip(1).Subscribe(_ => set.Add("Viewport"));
            target.GetObservable(ScrollViewer.ExtentProperty).Skip(1).Subscribe(_ => set.Add("Extent"));

            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new[] { "Viewport", "Extent" }, set);
        }

        [Fact]
        public void Should_Correctly_Arrange_Child_Larger_Than_Viewport()
        {
            var child = new Canvas { MinWidth = 150, MinHeight = 150 };
            var target = new ScrollContentPresenter { Content = child, };

            target.UpdateChild();
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(150, 150), child.Bounds.Size);
        }

        [Fact]
        public void Arrange_Should_Constrain_Child_Width_When_CanHorizontallyScroll_False()
        {
            var child = new WrapPanel
            {
                Children =
                {
                    new Border { Width = 40, Height = 50 },
                    new Border { Width = 40, Height = 50 },
                    new Border { Width = 40, Height = 50 },
                }
            };

            var target = new ScrollContentPresenter
            {
                Content = child,
                CanHorizontallyScroll = false,
            };

            target.UpdateChild();
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(100, child.Bounds.Width);
        }

        [Fact]
        public void Extent_Should_Include_Content_Margin()
        {
            var target = new ScrollContentPresenter
            {
                Content = new Border
                {
                    Width = 100,
                    Height = 100,
                    Margin = new Thickness(5),
                }
            };

            target.UpdateChild();
            target.Measure(new Size(50, 50));
            target.Arrange(new Rect(0, 0, 50, 50));

            Assert.Equal(new Size(110, 110), target.Extent);
        }

        [Fact]
        public void Extent_Should_Include_Content_Margin_Scaled_With_Layout_Rounding()
        {
            var root = new TestRoot
            {
                LayoutScaling = 1.25,
                UseLayoutRounding = true
            };

            var target = new ScrollContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Content = new Border
                {
                    Width = 200,
                    Height = 200,
                    Margin = new Thickness(2)
                }
            };

            root.Child = target;
            target.UpdateChild();
            target.Measure(new Size(1000, 1000));
            target.Arrange(new Rect(0, 0, 1000, 1000));

            Assert.Equal(new Size(203.2, 203.2), target.Viewport);
            Assert.Equal(new Size(203.2, 203.2), target.Extent);
        }

        [Fact]
        public void Extent_Should_Be_Rounded_To_Viewport_When_Close()
        {
            var root = new TestRoot
            {
                LayoutScaling = 1.75,
                UseLayoutRounding = true
            };

            var target = new ScrollContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Content = new Border
                {
                    Width = 164.57142857142858,
                    Height = 164.57142857142858,
                    Margin = new Thickness(6)
                }
            };

            root.Child = target;
            target.UpdateChild();
            target.Measure(new Size(1000, 1000));
            target.Arrange(new Rect(0, 0, 1000, 1000));

            Assert.Equal(new Size(176.00000000000003, 176.00000000000003), target.Child!.DesiredSize);
            Assert.Equal(new Size(176, 176), target.Viewport);
            Assert.Equal(new Size(176, 176), target.Extent);
        }

        [Fact]
        public void Extent_Width_Should_Be_Arrange_Width_When_CanScrollHorizontally_False()
        {
            var child = new WrapPanel
            {
                Children =
                {
                    new Border { Width = 40, Height = 50 },
                    new Border { Width = 40, Height = 50 },
                    new Border { Width = 40, Height = 50 },
                }
            };

            var target = new ScrollContentPresenter
            {
                Content = child,
                CanHorizontallyScroll = false,
            };

            target.UpdateChild();
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(100, 100), target.Extent);
        }

        [Fact]
        public void Setting_Offset_Should_Invalidate_Arrange()
        {
            var target = new ScrollContentPresenter
            {
                Content = new Border { Width = 140, Height = 150 }
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));
            target.Offset = new Vector(10, 100);

            Assert.True(target.IsMeasureValid);
            Assert.False(target.IsArrangeValid);
        }

        [Fact]
        public void BringDescendantIntoView_Should_Update_Offset()
        {
            var target = new ScrollContentPresenter
            {
                Width = 100,
                Height = 100,
                Content = new Border
                {
                    Width = 200,
                    Height = 200,
                }
            };

            target.UpdateChild();
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));
            target.BringDescendantIntoView(target.Child, new Rect(200, 200, 0, 0));

            Assert.Equal(new Vector(100, 100), target.Offset);
        }

        [Fact]
        public void BringDescendantIntoView_Should_Handle_Child_Margin()
        {
            Border border;
            var target = new ScrollContentPresenter
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                Width = 100,
                Height = 100,
                Content = new Decorator
                {
                    Margin = new Thickness(50),
                    Child = border = new Border
                    {
                        Width = 200,
                        Height = 200,
                    }
                }
            };

            target.UpdateChild();
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));
            target.BringDescendantIntoView(border, new Rect(200, 200, 0, 0));

            Assert.Equal(new Vector(150, 150), target.Offset);
        }

        private class TestControl : Control
        {
            public Size AvailableSize { get; private set; }

            protected override Size MeasureOverride(Size availableSize)
            {
                AvailableSize = availableSize;
                return new Size(150, 150);
            }
        }
    }
}
