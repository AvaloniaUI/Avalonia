// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
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

            target.UpdateChild();
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

            target.UpdateChild();
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

            target.UpdateChild();
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

            target.UpdateChild();
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

            target.UpdateChild();
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
                Content = content = new Border
                {
                    Width = 150,
                    Height = 150,
                },
                Offset = new Vector(25, 25),
            };

            target.UpdateChild();
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
                Content = child,
                [ScrollContentPresenter.CanScrollHorizontallyProperty] = false,
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