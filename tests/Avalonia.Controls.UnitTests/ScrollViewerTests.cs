using System;
using System.Collections.Generic;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ScrollViewerTests
    {
        [Fact]
        public void Content_Is_Created()
        {
            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
                Content = "Foo",
            };

            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            Assert.IsType<TextBlock>(target.Presenter.Child);
        }

        [Fact]
        public void CanHorizontallyScroll_Should_Track_HorizontalScrollBarVisibility()
        {
            var target = new ScrollViewer();
            var values = new List<bool>();

            target.GetObservable(ScrollViewer.CanHorizontallyScrollProperty).Subscribe(x => values.Add(x));
            target.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            target.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            Assert.Equal(new[] { false, true }, values);
        }

        [Fact]
        public void CanVerticallyScroll_Should_Track_VerticalScrollBarVisibility()
        {
            var target = new ScrollViewer();
            var values = new List<bool>();

            target.GetObservable(ScrollViewer.CanVerticallyScrollProperty).Subscribe(x => values.Add(x));
            target.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            target.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            Assert.Equal(new[] { true, false, true }, values);
        }

        [Fact]
        public void Offset_Should_Be_Coerced_To_Viewport()
        {
            var target = new ScrollViewer();
            target.SetValue(ScrollViewer.ExtentProperty, new Size(20, 20));
            target.SetValue(ScrollViewer.ViewportProperty, new Size(10, 10));
            target.Offset = new Vector(12, 12);

            Assert.Equal(new Vector(10, 10), target.Offset);
        }

        [Fact]
        public void Test_ScrollToHome()
        {
            var target = new ScrollViewer();
            target.SetValue(ScrollViewer.ExtentProperty, new Size(50, 50));
            target.SetValue(ScrollViewer.ViewportProperty, new Size(10, 10));
            target.Offset = new Vector(25, 25);
            target.ScrollToHome();

            Assert.Equal(new Vector(0, 0), target.Offset);
        }

        [Fact]
        public void Test_ScrollToEnd()
        {
            var target = new ScrollViewer();
            target.SetValue(ScrollViewer.ExtentProperty, new Size(50, 50));
            target.SetValue(ScrollViewer.ViewportProperty, new Size(10, 10));
            target.Offset = new Vector(25, 25);
            target.ScrollToEnd();

            Assert.Equal(new Vector(0, 40), target.Offset);
        }

        [Fact]
        public void SmallChange_Should_Be_16()
        {
            var target = new ScrollViewer();

            Assert.Equal(new Size(16, 16), target.SmallChange);
        }

        [Fact]
        public void LargeChange_Should_Be_Viewport()
        {
            var target = new ScrollViewer();

            target.SetValue(ScrollViewer.ViewportProperty, new Size(104, 143));
            Assert.Equal(new Size(104, 143), target.LargeChange);
        }

        [Fact]
        public void SmallChange_Should_Come_From_ILogicalScrollable_If_Present()
        {
            var child = new Mock<Control>();
            var logicalScroll = child.As<ILogicalScrollable>();

            logicalScroll.Setup(x => x.IsLogicalScrollEnabled).Returns(true);
            logicalScroll.Setup(x => x.ScrollSize).Returns(new Size(12, 43));

            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
                Content = child.Object,
            };

            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            Assert.Equal(new Size(12, 43), target.SmallChange);
        }

        [Fact]
        public void LargeChange_Should_Come_From_ILogicalScrollable_If_Present()
        {
            var child = new Mock<Control>();
            var logicalScroll = child.As<ILogicalScrollable>();

            logicalScroll.Setup(x => x.IsLogicalScrollEnabled).Returns(true);
            logicalScroll.Setup(x => x.PageScrollSize).Returns(new Size(45, 67));

            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
                Content = child.Object,
            };

            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            Assert.Equal(new Size(45, 67), target.LargeChange);
        }

        [Fact]
        public void Changing_Extent_Should_Raise_ScrollChanged()
        {
            var target = new ScrollViewer();
            var root = new TestRoot(target);
            var raised = 0;

            target.SetValue(ScrollViewer.ExtentProperty, new Size(100, 100));
            target.SetValue(ScrollViewer.ViewportProperty, new Size(50, 50));
            target.Offset = new Vector(10, 10);

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.ScrollChanged += (s, e) =>
            {
                Assert.Equal(new Vector(11, 12), e.ExtentDelta);
                Assert.Equal(default, e.OffsetDelta);
                Assert.Equal(default, e.ViewportDelta);
                ++raised;
            };

            target.SetValue(ScrollViewer.ExtentProperty, new Size(111, 112));

            Assert.Equal(0, raised);

            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Changing_Offset_Should_Raise_ScrollChanged()
        {
            var target = new ScrollViewer();
            var root = new TestRoot(target);
            var raised = 0;

            target.SetValue(ScrollViewer.ExtentProperty, new Size(100, 100));
            target.SetValue(ScrollViewer.ViewportProperty, new Size(50, 50));
            target.Offset = new Vector(10, 10);

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.ScrollChanged += (s, e) =>
            {
                Assert.Equal(default, e.ExtentDelta);
                Assert.Equal(new Vector(12, 14), e.OffsetDelta);
                Assert.Equal(default, e.ViewportDelta);
                ++raised;
            };

            target.Offset = new Vector(22, 24);

            Assert.Equal(0, raised);

            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Changing_Viewport_Should_Raise_ScrollChanged()
        {
            var target = new ScrollViewer();
            var root = new TestRoot(target);
            var raised = 0;

            target.SetValue(ScrollViewer.ExtentProperty, new Size(100, 100));
            target.SetValue(ScrollViewer.ViewportProperty, new Size(50, 50));
            target.Offset = new Vector(10, 10);

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.ScrollChanged += (s, e) =>
            {
                Assert.Equal(default, e.ExtentDelta);
                Assert.Equal(default, e.OffsetDelta);
                Assert.Equal(new Vector(6, 8), e.ViewportDelta);
                ++raised;
            };

            target.SetValue(ScrollViewer.ViewportProperty, new Size(56, 58));

            Assert.Equal(0, raised);

            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, raised);
        }

        private Control CreateTemplate(ScrollViewer control, INameScope scope)
        {
            return new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(1, GridUnitType.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                RowDefinitions = new RowDefinitions
                {
                    new RowDefinition(1, GridUnitType.Star),
                    new RowDefinition(GridLength.Auto),
                },
                Children =
                {
                    new ScrollContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                        [~~ScrollContentPresenter.ExtentProperty] = control[~~ScrollViewer.ExtentProperty],
                        [~~ScrollContentPresenter.OffsetProperty] = control[~~ScrollViewer.OffsetProperty],
                        [~~ScrollContentPresenter.ViewportProperty] = control[~~ScrollViewer.ViewportProperty],
                        [~ScrollContentPresenter.CanHorizontallyScrollProperty] = control[~ScrollViewer.CanHorizontallyScrollProperty],
                    }.RegisterInNameScope(scope),
                    new ScrollBar
                    {
                        Name = "horizontalScrollBar",
                        Orientation = Orientation.Horizontal,
                        [~RangeBase.MaximumProperty] = control[~ScrollViewer.HorizontalScrollBarMaximumProperty],
                        [~~RangeBase.ValueProperty] = control[~~ScrollViewer.HorizontalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.HorizontalScrollBarViewportSizeProperty],
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.HorizontalScrollBarVisibilityProperty],
                        [Grid.RowProperty] = 1,
                    }.RegisterInNameScope(scope),
                    new ScrollBar
                    {
                        Name = "verticalScrollBar",
                        Orientation = Orientation.Vertical,
                        [~RangeBase.MaximumProperty] = control[~ScrollViewer.VerticalScrollBarMaximumProperty],
                        [~~RangeBase.ValueProperty] = control[~~ScrollViewer.VerticalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.VerticalScrollBarViewportSizeProperty],
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.VerticalScrollBarVisibilityProperty],
                        [Grid.ColumnProperty] = 1,
                    }.RegisterInNameScope(scope),
                },
            };
        }
    }
}
