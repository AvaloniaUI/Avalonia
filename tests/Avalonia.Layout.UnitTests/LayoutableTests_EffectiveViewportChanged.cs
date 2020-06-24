using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Layout.UnitTests
{
    public class LayoutableTests_EffectiveViewportChanged
    {
        [Fact]
        public void EffectiveViewportChanged_Not_Raised_When_Control_Added_To_Tree()
        {
            var root = new TestRoot();
            var canvas = new Canvas();
            var raised = 0;

            canvas.EffectiveViewportChanged += (s, e) =>
            {
                ++raised;
            };

            root.Child = canvas;

            Assert.Equal(0, raised);
        }

        [Fact]
        public void EffectiveViewportChanged_Raised_Before_LayoutUpdated()
        {
            var root = new TestRoot();
            var canvas = new Canvas();
            var raised = 0;
            var layoutUpdatedRaised = 0;

            canvas.LayoutUpdated += (s, e) =>
            {
                Assert.Equal(1, raised);
                ++layoutUpdatedRaised;
            };

            canvas.EffectiveViewportChanged += (s, e) =>
            {
                ++raised;
            };

            root.Child = canvas;
            root.LayoutManager.ExecuteInitialLayoutPass(root);

            Assert.Equal(1, layoutUpdatedRaised);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Invalidating_In_Handler_Causes_Layout_To_Be_Rerun_Before_LayoutUpdated()
        {
            var root = new TestRoot();
            var canvas = new TestCanvas();
            var raised = 0;
            var layoutUpdatedRaised = 0;

            canvas.LayoutUpdated += (s, e) =>
            {
                Assert.Equal(2, canvas.MeasureCount);
                Assert.Equal(2, canvas.ArrangeCount);
                ++layoutUpdatedRaised;
            };

            canvas.EffectiveViewportChanged += (s, e) =>
            {
                canvas.InvalidateMeasure();
                ++raised;
            };

            root.Child = canvas;
            root.LayoutManager.ExecuteInitialLayoutPass(root);

            Assert.Equal(1, raised);
            Assert.Equal(1, layoutUpdatedRaised);
        }

        [Fact]
        public void Viewport_Extends_Beyond_Centered_Control()
        {
            var root = new TestRoot
            {
                Width = 1200,
                Height = 900,
            };

            var canvas = new Canvas
            {
                Width = 52,
                Height = 52,
            };
            var raised = 0;

            canvas.EffectiveViewportChanged += (s, e) =>
            {
                Assert.Equal(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                ++raised;
            };

            root.Child = canvas;
            root.LayoutManager.ExecuteInitialLayoutPass(root);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Viewport_Extends_Beyond_Nested_Centered_Control()
        {
            var root = new TestRoot
            {
                Width = 1200,
                Height = 900,
            };

            var canvas = new Canvas
            {
                Width = 52,
                Height = 52,
            };

            var outer = new Border
            {
                Width = 100,
                Height = 100,
                Child = canvas,
            };

            var raised = 0;

            canvas.EffectiveViewportChanged += (s, e) =>
            {
                Assert.Equal(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                ++raised;
            };

            root.Child = outer;
            root.LayoutManager.ExecuteInitialLayoutPass(root);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void ScrollViewer_Determines_EffectiveViewport()
        {
            var root = new TestRoot
            {
                Width = 1200,
                Height = 900,
            };

            var canvas = new Canvas
            {
                Width = 200,
                Height = 200,
            };

            var outer = new ScrollViewer
            {
                Width = 100,
                Height = 100,
                Content = canvas,
                Template = ScrollViewerTemplate(),
            };

            var raised = 0;

            canvas.EffectiveViewportChanged += (s, e) =>
            {
                Assert.Equal(new Rect(0, 0, 100, 100), e.EffectiveViewport);
                ++raised;
            };

            root.Child = outer;
            root.LayoutManager.ExecuteInitialLayoutPass(root);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Scrolled_ScrollViewer_Determines_EffectiveViewport()
        {
            var root = new TestRoot
            {
                Width = 1200,
                Height = 900,
            };

            var canvas = new Canvas
            {
                Width = 200,
                Height = 200,
            };

            var outer = new ScrollViewer
            {
                Width = 100,
                Height = 100,
                Content = canvas,
                Template = ScrollViewerTemplate(),
            };

            var raised = 0;

            root.Child = outer;
            root.LayoutManager.ExecuteInitialLayoutPass(root);

            canvas.EffectiveViewportChanged += (s, e) =>
            {
                Assert.Equal(new Rect(0, 10, 100, 100), e.EffectiveViewport);
                ++raised;
            };
            
            outer.Offset = new Vector(0, 10);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Moving_Parent_Updates_EffectiveViewport()
        {
            var root = new TestRoot
            {
                Width = 1200,
                Height = 900,
            };

            var canvas = new Canvas
            {
                Width = 100,
                Height = 100,
            };

            var outer = new Border
            {
                Width = 200,
                Height = 200,
                Child = canvas,
            };

            var raised = 0;

            root.Child = outer;
            root.LayoutManager.ExecuteInitialLayoutPass(root);

            canvas.EffectiveViewportChanged += (s, e) =>
            {
                Assert.Equal(new Rect(-554, -400, 1200, 900), e.EffectiveViewport);
                ++raised;
            };

            // Change the parent margin to move it.
            outer.Margin = new Thickness(8, 0, 0, 0);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, raised);
        }

        private IControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((control, scope) => new Grid
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
                        [~ScrollContentPresenter.CanVerticallyScrollProperty] = control[~ScrollViewer.CanVerticallyScrollProperty],
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
            });
        }


        private class TestCanvas : Canvas
        {
            public int MeasureCount { get; private set; }
            public int ArrangeCount { get; private set; }

            protected override Size MeasureOverride(Size availableSize)
            {
                ++MeasureCount;
                return base.MeasureOverride(availableSize);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                ++ArrangeCount;
                return base.ArrangeOverride(finalSize);
            }
        }
    }
}
