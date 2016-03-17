// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class ScrollContentPresenterTests_IScrollable
    {
        [Fact]
        public void Measure_Should_Pass_Unchanged_Bounds_To_IScrollable()
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
        public void Arrange_Should_Not_Offset_IScrollable_Bounds()
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
        public void Arrange_Should_Not_Set_Viewport_And_Extent_With_IScrollable()
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

            Assert.NotNull(scrollable.InvalidateScroll);
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

            Assert.Null(scrollable.InvalidateScroll);
        }

        [Fact]
        public void Extent_Offset_And_Viewport_Should_Be_Read_From_IScrollable()
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
        public void Offset_Should_Be_Written_To_IScrollable()
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
        public void Offset_Should_Not_Be_Written_To_IScrollable_After_Removal()
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

        private class TestScrollable : Control, IScrollable
        {
            private Size _extent;
            private Vector _offset;
            private Size _viewport;

            public Size AvailableSize { get; private set; }
            public Action InvalidateScroll { get; set; }

            public Size Extent
            {
                get { return _extent; }
                set
                {
                    _extent = value;
                    InvalidateScroll?.Invoke();
                }
            }

            public Vector Offset
            {
                get { return _offset; }
                set
                {
                    _offset = value;
                    InvalidateScroll?.Invoke();
                }
            }

            public Size Viewport
            {
                get { return _viewport; }
                set
                {
                    _viewport = value;
                    InvalidateScroll?.Invoke();
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

            protected override Size MeasureOverride(Size availableSize)
            {
                AvailableSize = availableSize;
                return new Size(150, 150);
            }
        }
    }
}