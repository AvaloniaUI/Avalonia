using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests;

public class ScrollViewerTests_ILogicalScrollable : ScopedTestBase
{
    [Fact]
    public void Extent_Offset_And_Viewport_Should_Be_Read_From_ILogicalScrollable()
    {
        var scrollable = new TestScrollable
        {
            Extent = new Size(100, 100),
            Offset = new Vector(50, 50),
            Viewport = new Size(25, 25),
        };

        var target = CreateTarget(scrollable);

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

    private static ScrollViewer CreateTarget(object? content)
    {
        var result = new ScrollViewer
        {
            Content = content,
        };

        var root = new TestRoot
        {
            Resources =
            {
                { typeof(ScrollViewer), CreateScrollViewerTheme() },
            },
            Child = result,
        };

        root.LayoutManager.ExecuteInitialLayoutPass();
        return result;
    }

    private static ControlTheme CreateScrollViewerTheme()
    {
        return new ControlTheme(typeof(ScrollViewer))
        {
            Setters =
            {
                new Setter(TreeView.TemplateProperty, CreateScrollViewerTemplate()),
            },
        };
    }

    private static FuncControlTemplate CreateScrollViewerTemplate()
    {
        return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
            new Panel
            {
                Children =
                {
                    new ScrollContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                    }.RegisterInNameScope(scope),
                }
            });
    }

    private class TestScrollable : Control, ILogicalScrollable
    {
        private Size _extent;
        private Vector _offset;
        private Size _viewport;
        private EventHandler? _scrollInvalidated;

        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public bool IsLogicalScrollEnabled { get; set; } = true;
        public Size AvailableSize { get; private set; }

        public bool HasScrollInvalidatedSubscriber => _scrollInvalidated != null;
        
        public event EventHandler? ScrollInvalidated
        {
            add => _scrollInvalidated += value;
            remove => _scrollInvalidated -= value;
        }

        public Size Extent
        {
            get => _extent;
            set
            {
                _extent = value;
                _scrollInvalidated?.Invoke(this, EventArgs.Empty);
            }
        }

        public Vector Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                _scrollInvalidated?.Invoke(this, EventArgs.Empty);
            }
        }

        public Size Viewport
        {
            get => _viewport;
            set
            {
                _viewport = value;
                _scrollInvalidated?.Invoke(this, EventArgs.Empty);
            }
        }

        public Size ScrollSize => new(double.PositiveInfinity, 1);
        public Size PageScrollSize => new(double.PositiveInfinity, Viewport.Height);

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

        public Control? GetControlInDirection(NavigationDirection direction, Control? from)
        {
            throw new NotImplementedException();
        }
    }
}
