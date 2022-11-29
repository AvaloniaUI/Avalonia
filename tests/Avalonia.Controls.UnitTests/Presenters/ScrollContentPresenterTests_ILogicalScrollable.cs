using System;
using System.Reactive.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ScrollContentPresenterTests_ILogicalScrollable
    {
        [Fact]
        public void Measure_Should_Pass_Unchanged_Bounds_To_ILogicalScrollable()
        {
            var scrollable = new TestScrollable();
            var target = new ScrollContentPresenter
            {
                Content = scrollable,
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(100, 100), scrollable.AvailableSize);
        }

        [Fact]
        public void Arrange_Should_Not_Offset_ILogicalScrollable_Bounds()
        {
            var scrollable = new TestScrollable
            {
                Extent = new Size(100, 100),
                Offset = new Vector(50, 50),
                Viewport = new Size(25, 25),
            };

            var target = new ScrollContentPresenter
            {
                Content = scrollable,
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 0, 100, 100), scrollable.Bounds);
        }

        [Fact]
        public void Arrange_Should_Offset_ILogicalScrollable_Bounds_When_Logical_Scroll_Disabled()
        {
            var scrollable = new TestScrollable
            {
                IsLogicalScrollEnabled = false,
            };

            var target = new ScrollContentPresenter
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                Content = scrollable,
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            target.Offset = new Vector(25, 25);
            
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(-25, -25, 150, 150), scrollable.Bounds);
        }

        [Fact]
        public void Arrange_Should_Not_Set_Viewport_And_Extent_With_ILogicalScrollable()
        {
            var target = new ScrollContentPresenter
            {
                Content = new TestScrollable()
            };

            var changed = false;

            target.UpdateChild();
            target.Measure(new Size(100, 100));

            target.GetObservable(ScrollViewer.ViewportProperty).Skip(1).Subscribe(_ => changed = true);
            target.GetObservable(ScrollViewer.ExtentProperty).Skip(1).Subscribe(_ => changed = true);

            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.False(changed);
        }

        [Fact]
        public void InvalidateScroll_Should_Be_Set_When_Set_As_Content()
        {
            var scrollable = new TestScrollable();
            var target = new ScrollContentPresenter
            {
                Content = scrollable
            };

            target.UpdateChild();

            Assert.True(scrollable.HasScrollInvalidatedSubscriber);
        }

        [Fact]
        public void InvalidateScroll_Should_Be_Cleared_When_Removed_From_Content()
        {
            var scrollable = new TestScrollable();
            var target = new ScrollContentPresenter
            {
                Content = scrollable
            };

            target.UpdateChild();
            target.Content = null;
            target.UpdateChild();

            Assert.False(scrollable.HasScrollInvalidatedSubscriber);
        }

        [Fact]
        public void Extent_Offset_And_Viewport_Should_Be_Read_From_ILogicalScrollable()
        {
            var scrollable = new TestScrollable
            {
                Extent = new Size(100, 100),
                Offset = new Vector(50, 50),
                Viewport = new Size(25, 25),
            };

            var target = new ScrollContentPresenter
            {
                Content = scrollable
            };

            target.UpdateChild();

            Assert.Equal(scrollable.Extent, target.Extent);
            Assert.Equal(scrollable.Offset, target.Offset);
            Assert.Equal(scrollable.Viewport, target.Viewport);

            scrollable.Extent = new Size(200, 200);
            scrollable.Offset = new Vector(100, 100);
            scrollable.Viewport = new Size(50, 50);

            Assert.Equal(scrollable.Extent, target.Extent);
            Assert.Equal(scrollable.Offset, target.Offset);
            Assert.Equal(scrollable.Viewport, target.Viewport);
        }

        [Fact]
        public void Offset_Should_Be_Written_To_ILogicalScrollable()
        {
            var scrollable = new TestScrollable
            {
                Extent = new Size(100, 100),
                Offset = new Vector(50, 50),
            };

            var target = new ScrollContentPresenter
            {
                Content = scrollable
            };

            target.UpdateChild();
            target.Offset = new Vector(25, 25);

            Assert.Equal(target.Offset, scrollable.Offset);
        }

        [Fact]
        public void Offset_Should_Not_Be_Written_To_ILogicalScrollable_After_Removal()
        {
            var scrollable = new TestScrollable
            {
                Extent = new Size(100, 100),
                Offset = new Vector(50, 50),
            };

            var target = new ScrollContentPresenter
            {
                Content = scrollable
            };

            target.Content = null;
            target.Offset = new Vector(25, 25);

            Assert.Equal(new Vector(50, 50), scrollable.Offset);
        }

        [Fact]
        public void Toggling_IsLogicalScrollEnabled_Should_Update_State()
        {
            var scrollable = new TestScrollable
            {
                Extent = new Size(100, 100),
                Offset = new Vector(50, 50),
                Viewport = new Size(25, 25),
            };

            var target = new ScrollContentPresenter
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                Content = scrollable,
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(scrollable.Extent, target.Extent);
            Assert.Equal(scrollable.Offset, target.Offset);
            Assert.Equal(scrollable.Viewport, target.Viewport);
            Assert.Equal(new Rect(0, 0, 100, 100), scrollable.Bounds);

            scrollable.IsLogicalScrollEnabled = false;
            scrollable.RaiseScrollInvalidated(EventArgs.Empty);
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(150, 150), target.Extent);
            Assert.Equal(new Vector(0, 0), target.Offset);
            Assert.Equal(new Size(100, 100), target.Viewport);
            Assert.Equal(new Rect(0, 0, 150, 150), scrollable.Bounds);

            scrollable.IsLogicalScrollEnabled = true;
            scrollable.RaiseScrollInvalidated(EventArgs.Empty);
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(scrollable.Extent, target.Extent);
            Assert.Equal(scrollable.Offset, target.Offset);
            Assert.Equal(scrollable.Viewport, target.Viewport);
            Assert.Equal(new Rect(0, 0, 100, 100), scrollable.Bounds);
        }

        [Fact]
        public void Changing_Content_Should_Update_State()
        {
            var logicalScrollable = new TestScrollable
            {
                Extent = new Size(100, 100),
                Offset = new Vector(50, 50),
                Viewport = new Size(25, 25),
            };

            var nonLogicalScrollable = new TestScrollable
            {
                IsLogicalScrollEnabled = false,
            };

            var target = new ScrollContentPresenter
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                Content = logicalScrollable,
            };

            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(logicalScrollable.Extent, target.Extent);
            Assert.Equal(logicalScrollable.Offset, target.Offset);
            Assert.Equal(logicalScrollable.Viewport, target.Viewport);
            Assert.Equal(new Rect(0, 0, 100, 100), logicalScrollable.Bounds);

            target.Content = nonLogicalScrollable;
            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(150, 150), target.Extent);
            Assert.Equal(new Vector(0, 0), target.Offset);
            Assert.Equal(new Size(100, 100), target.Viewport);
            Assert.Equal(new Rect(0, 0, 150, 150), nonLogicalScrollable.Bounds);

            target.Content = logicalScrollable;
            target.UpdateChild();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(logicalScrollable.Extent, target.Extent);
            Assert.Equal(logicalScrollable.Offset, target.Offset);
            Assert.Equal(logicalScrollable.Viewport, target.Viewport);
            Assert.Equal(new Rect(0, 0, 100, 100), logicalScrollable.Bounds);
        }

        [Fact]
        public void Should_Set_ILogicalScrolable_CanHorizontallyScroll()
        {
            var logicalScrollable = new TestScrollable();
            var target = new ScrollContentPresenter { Content = logicalScrollable };

            target.UpdateChild();
            Assert.False(logicalScrollable.CanHorizontallyScroll);
            target.CanHorizontallyScroll = true;
            Assert.True(logicalScrollable.CanHorizontallyScroll);
        }

        [Fact]
        public void Should_Set_ILogicalScrolable_CanVerticallyScroll()
        {
            var logicalScrollable = new TestScrollable();
            var target = new ScrollContentPresenter { Content = logicalScrollable };

            target.UpdateChild();
            Assert.False(logicalScrollable.CanVerticallyScroll);
            target.CanVerticallyScroll = true;
            Assert.True(logicalScrollable.CanVerticallyScroll);
        }

        private class TestScrollable : Control, ILogicalScrollable
        {
            private Size _extent;
            private Vector _offset;
            private Size _viewport;
            private EventHandler _scrollInvalidated;

            public bool CanHorizontallyScroll { get; set; }
            public bool CanVerticallyScroll { get; set; }
            public bool IsLogicalScrollEnabled { get; set; } = true;
            public Size AvailableSize { get; private set; }

            public bool HasScrollInvalidatedSubscriber => _scrollInvalidated != null;
            
            public event EventHandler ScrollInvalidated
            {
                add => _scrollInvalidated += value;
                remove => _scrollInvalidated -= value;
            }

            public Size Extent
            {
                get { return _extent; }
                set
                {
                    _extent = value;
                    _scrollInvalidated?.Invoke(this, EventArgs.Empty);
                }
            }

            public Vector Offset
            {
                get { return _offset; }
                set
                {
                    _offset = value;
                    _scrollInvalidated?.Invoke(this, EventArgs.Empty);
                }
            }

            public Size Viewport
            {
                get { return _viewport; }
                set
                {
                    _viewport = value;
                    _scrollInvalidated?.Invoke(this, EventArgs.Empty);
                }
            }

            public Size ScrollSize
            {
                get
                {
                    return new Size(double.PositiveInfinity, 1);
                }
            }

            public Size PageScrollSize
            {
                get
                {
                    return new Size(double.PositiveInfinity, Viewport.Height);
                }
            }

            public bool BringIntoView(Control target, Rect targetRect)
            {
                throw new NotImplementedException();
            }

            public void RaiseScrollInvalidated(EventArgs e)
            {
                _scrollInvalidated?.Invoke(this, e);
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                AvailableSize = availableSize;
                return new Size(150, 150);
            }

            public Control GetControlInDirection(NavigationDirection direction, Control from)
            {
                throw new NotImplementedException();
            }
        }
    }
}
