using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ScrollViewerTests : ScopedTestBase
    {
        private readonly MouseTestHelper _mouse = new();

        [Fact]
        public void Content_Is_Created()
        {
            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
                Content = "Foo",
            };

            InitializeScrollViewer(target);

            Assert.IsType<TextBlock>(target.Presenter.Child);
        }

        [Fact]
        public void Offset_Should_Be_Coerced_To_Viewport()
        {
            var target = new ScrollViewer
            {
                Extent = new Size(20, 20),
                Viewport = new Size(10, 10),
                Offset = new Vector(12, 12)
            };

            Assert.Equal(new Vector(10, 10), target.Offset);
        }

        [Fact]
        public void Test_ScrollToHome()
        {
            var target = new ScrollViewer
            {
                Extent = new Size(50, 50),
                Viewport = new Size(10, 10),
                Offset = new Vector(25, 25)
            };
            target.ScrollToHome();

            Assert.Equal(new Vector(0, 0), target.Offset);
        }

        [Fact]
        public void Test_ScrollToEnd()
        {
            var target = new ScrollViewer
            {
                Extent = new Size(50, 50),
                Viewport = new Size(10, 10),
                Offset = new Vector(25, 25)
            };
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
            var target = new ScrollViewer
            {
                Viewport = new Size(104, 143)
            };
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

            InitializeScrollViewer(target);

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

            InitializeScrollViewer(target);

            Assert.Equal(new Size(45, 67), target.LargeChange);
        }

        [Fact]
        public void Changing_Extent_Should_Raise_ScrollChanged()
        {
            var target = new ScrollViewer();
            var root = new TestRoot(target);
            var raised = 0;

            target.Extent = new Size(100, 100);
            target.Viewport = new Size(50, 50);
            target.Offset = new Vector(10, 10);

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.ScrollChanged += (s, e) =>
            {
                Assert.Equal(new Vector(11, 12), e.ExtentDelta);
                Assert.Equal(default, e.OffsetDelta);
                Assert.Equal(default, e.ViewportDelta);
                ++raised;
            };

            target.Extent = new Size(111, 112);

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

            target.Extent = new Size(100, 100);
            target.Viewport = new Size(50, 50);
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

            target.Extent = new Size(100, 100);
            target.Viewport = new Size(50, 50);
            target.Offset = new Vector(10, 10);

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.ScrollChanged += (s, e) =>
            {
                Assert.Equal(default, e.ExtentDelta);
                Assert.Equal(default, e.OffsetDelta);
                Assert.Equal(new Vector(6, 8), e.ViewportDelta);
                ++raised;
            };

            target.Viewport = new Size(56, 58);

            Assert.Equal(0, raised);

            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Reducing_Extent_Should_Constrain_Offset()
        {
            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
            };
            var root = new TestRoot(target);
            var raised = 0;

            target.Extent = new (100, 100);
            target.Viewport = new(50, 50);
            target.Offset = new Vector(50, 50);

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.ScrollChanged += (s, e) =>
            {
                Assert.Equal(new Vector(-30, -30), e.ExtentDelta);
                Assert.Equal(new Vector(-30, -30), e.OffsetDelta);
                Assert.Equal(default, e.ViewportDelta);
                ++raised;
            };

            target.Extent = new(70, 70);

            Assert.Equal(0, raised);

            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, raised);
            Assert.Equal(new Vector(20, 20), target.Offset); 
        }

        [Fact]
        public void Scroll_Does_Not_Jump_When_Viewport_Becomes_Smaller_While_Dragging_ScrollBar_Thumb()
        {
            var content = new TestContent
            {
                MeasureSize = new Size(1000, 10000),
            };

            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
                Content = content,
            };
            var root = new TestRoot(target);

            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal(new Size(1000, 10000), target.Extent);
            Assert.Equal(new Size(1000, 1000), target.Viewport);

            // We're working in absolute coordinates (i.e. relative to the root) and clicking on
            // the center of the vertical thumb.
            var thumb = GetVerticalThumb(target);
            var p = GetRootPoint(thumb, thumb.Bounds.Center);

            // Press the mouse button in the center of the thumb.
            _mouse.Down(thumb, position: p);
            root.LayoutManager.ExecuteLayoutPass();

            // Drag the thumb down 300 pixels.
            _mouse.Move(thumb, p += new Vector(0, 300));
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(new Vector(0, 3000), target.Offset);
            Assert.Equal(300, thumb.Bounds.Top);

            // Now the extent changes from 10,000 to 5000.
            content.MeasureSize /= 2;
            content.InvalidateMeasure();
            root.LayoutManager.ExecuteLayoutPass();

            // Due to the extent change, the thumb moves down but the value remains the same.
            Assert.Equal(600, thumb.Bounds.Top);
            Assert.Equal(new Vector(0, 3000), target.Offset);

            // Drag the thumb down another 100 pixels.
            _mouse.Move(thumb, p += new Vector(0, 100));
            root.LayoutManager.ExecuteLayoutPass();

            // The drag should not cause the offset/thumb to jump *up* to the current absolute
            // mouse position, i.e. it should move down in the direction of the drag even if the
            // absolute mouse position is now above the thumb.
            Assert.Equal(700, thumb.Bounds.Top);
            Assert.Equal(new Vector(0, 3500), target.Offset);
        }

        [Fact]
        public void Thumb_Does_Not_Become_Detached_From_Mouse_Position_When_Scrolling_Past_The_Start()
        {
            var content = new TestContent();
            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
                Content = content,
            };
            var root = new TestRoot(target);

            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal(new Size(1000, 2000), target.Extent);
            Assert.Equal(new Size(1000, 1000), target.Viewport);

            // We're working in absolute coordinates (i.e. relative to the root) and clicking on
            // the center of the vertical thumb.
            var thumb = GetVerticalThumb(target);
            var p = GetRootPoint(thumb, thumb.Bounds.Center);

            // Press the mouse button in the center of the thumb.
            _mouse.Down(thumb, position: p);
            root.LayoutManager.ExecuteLayoutPass();

            // Drag the thumb down 100 pixels.
            _mouse.Move(thumb, p += new Vector(0, 100));
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(new Vector(0, 200), target.Offset);
            Assert.Equal(100, thumb.Bounds.Top);

            // Drag the thumb up 200 pixels - 100 pixels past the top of the scrollbar.
            _mouse.Move(thumb, p -= new Vector(0, 200));
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(new Vector(0, 0), target.Offset);
            Assert.Equal(0, thumb.Bounds.Top);

            // Drag the thumb back down 200 pixels.
            _mouse.Move(thumb, p += new Vector(0, 200));
            root.LayoutManager.ExecuteLayoutPass();

            // We should now be back in the state after we first scrolled down 100 pixels.
            Assert.Equal(new Vector(0, 200), target.Offset);
            Assert.Equal(100, thumb.Bounds.Top);
        }

        [Fact]
        public void Deferred_Scrolling_Defers_Scrolling_Until_Pointer_Up()
        {
            var content = new TestContent();
            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
                IsDeferredScrollingEnabled = true,
                Content = content,
            };
            var root = new TestRoot(target);

            root.LayoutManager.ExecuteInitialLayoutPass();

            // We're working in absolute coordinates (i.e. relative to the root) and clicking on
            // the center of the vertical thumb.
            var thumb = GetVerticalThumb(target);
            var p = GetRootPoint(thumb, thumb.Bounds.Center);

            Assert.Equal(Vector.Zero, target.Offset);
            Assert.Equal(0, thumb.Bounds.Top);

            // Press the mouse button in the center of the thumb.
            _mouse.Down(thumb, position: p);
            root.LayoutManager.ExecuteLayoutPass();

            // Drag the thumb down 100 pixels.
            _mouse.Move(thumb, p += new Vector(0, 100));
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(Vector.Zero, target.Offset); // no change to scroll...
            Assert.Equal(100, thumb.Bounds.Top); // ...but the Thumb has moved

            // Release the mouse
            _mouse.Up(thumb, position: p);

            Assert.Equal(new Vector(0, 200), target.Offset);
            Assert.Equal(100, thumb.Bounds.Top);
        }

        [Fact]
        public void BringIntoViewOnFocusChange_Scrolls_Child_Control_Into_View_When_Focused()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);
            var content = new StackPanel
            {
                Children =
                {
                    new Button
                    {
                        Width = 100,
                        Height = 900,
                    },
                    new Button
                    {
                        Width = 100,
                        Height = 900,
                    },
                }
            };

            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
                Content = content,
            };
            var root = new TestRoot(target);

            root.LayoutManager.ExecuteInitialLayoutPass();

            var button = (Button)content.Children[1];
            button.Focus();

            Assert.Equal(new Vector(0, 800), target.Offset);
        }

        [Fact]
        public void BringIntoViewOnFocusChange_False_Does_Not_Scroll_Child_Control_Into_View_When_Focused()
        {
            var content = new StackPanel
            {
                Children =
                {
                    new Button
                    {
                        Width = 100,
                        Height = 900,
                    },
                    new Button
                    {
                        Width = 100,
                        Height = 900,
                    },
                }
            };

            var target = new ScrollViewer
            {
                Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
                Content = content,
            };
            var root = new TestRoot(target);

            root.LayoutManager.ExecuteInitialLayoutPass();

            var button = (Button)content.Children[1];
            button.Focus();

            Assert.Equal(new Vector(0, 0), target.Offset);
        }

        private Point GetRootPoint(Visual control, Point p)
        {
            if (control.GetVisualRoot() is Visual root &&
                control.TransformToVisual(root) is Matrix m)
            {
                return p.Transform(m);
            }

            throw new InvalidOperationException("Could not get the point in root coordinates.");
        }

        internal static Control CreateTemplate(ScrollViewer control, INameScope scope)
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
                    }.RegisterInNameScope(scope),
                    new ScrollBar
                    {
                        Name = "PART_HorizontalScrollBar",
                        Orientation = Orientation.Horizontal,
                        Template = new FuncControlTemplate<ScrollBar>(CreateScrollBarTemplate),
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.HorizontalScrollBarVisibilityProperty],
                        [Grid.RowProperty] = 1,
                    }.RegisterInNameScope(scope),
                    new ScrollBar
                    {
                        Name = "PART_VerticalScrollBar",
                        Orientation = Orientation.Vertical,
                        Template = new FuncControlTemplate<ScrollBar>(CreateScrollBarTemplate),
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.VerticalScrollBarVisibilityProperty],
                        [Grid.ColumnProperty] = 1,
                    }.RegisterInNameScope(scope),
                },
            };
        }

        private static Control CreateScrollBarTemplate(ScrollBar scrollBar, INameScope scope)
        {
            return new Border
            {
                Child = new Track
                {
                    Name = "track",
                    IsDirectionReversed = true,
                    [!Track.MinimumProperty] = scrollBar[!RangeBase.MinimumProperty],
                    [!Track.MaximumProperty] = scrollBar[!RangeBase.MaximumProperty],
                    [!!Track.ValueProperty] = scrollBar[!!RangeBase.ValueProperty],
                    [!Track.ViewportSizeProperty] = scrollBar[!ScrollBar.ViewportSizeProperty],
                    [!Track.OrientationProperty] = scrollBar[!ScrollBar.OrientationProperty],
                    [!Track.DeferThumbDragProperty] = scrollBar.TemplatedParent[!ScrollViewer.IsDeferredScrollingEnabledProperty],
                    Thumb = new Thumb
                    {
                        Template = new FuncControlTemplate<Thumb>(CreateThumbTemplate),
                    },
                }.RegisterInNameScope(scope),
            };
        }

        private static Control CreateThumbTemplate(Thumb control, INameScope scope)
        {
            return new Border
            {
                Background = Brushes.Gray,
            };
        }

        private Thumb GetVerticalThumb(ScrollViewer target)
        {
            var scrollbar = Assert.IsType<ScrollBar>(
                target.GetTemplateChildren().FirstOrDefault(x => x.Name == "PART_VerticalScrollBar"));
            var track = Assert.IsType<Track>(
                scrollbar.GetTemplateChildren().FirstOrDefault(x => x.Name == "track"));
            return Assert.IsType<Thumb>(track.Thumb);
        }

        private static void InitializeScrollViewer(ScrollViewer target)
        {
            target.ApplyTemplate();

            var presenter = (ScrollContentPresenter)target.Presenter;
            presenter.AttachToScrollViewer();
            presenter.UpdateChild();
        }

        private class TestContent : Control
        {
            public Size MeasureSize { get; set; } = new Size(1000, 2000);

            protected override Size MeasureOverride(Size availableSize)
            {
                return MeasureSize;
            }
        }
    }
}
