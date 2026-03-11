using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class CompositorInvalidationTests : CompositorTestsBase
{
    [Fact]
    public void Control_Should_Invalidate_Own_Rect_When_Added()
    {
        using (var s = new CompositorCanvas())
        {
            var control = new Border()
            {
                Background = Brushes.Red, Width = 20, Height = 10,
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50
            };
            s.Canvas.Children.Add(control);
            s.AssertRects(new Rect(30, 50, 20, 10));
        }
    }

    [Fact]
    public void Control_Should_Invalidate_Own_Rect_When_Removed()
    {
        using (var s = new CompositorCanvas())
        {
            var control = new Border()
            {
                Background = Brushes.Red, Width = 20, Height = 10,
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50
            };
            s.Canvas.Children.Add(control);
            s.RunJobs();
            s.Events.Rects.Clear();
            s.Canvas.Children.Remove(control);
            s.AssertRects(new Rect(30, 50, 20, 10));
        }
    }

    [Fact]
    public void Control_Should_Invalidate_Both_Own_Rects_When_Moved()
    {
        using (var s = new CompositorCanvas())
        {
            var control = new Border()
            {
                Background = Brushes.Red, Width = 20, Height = 10,
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50
            };
            s.Canvas.Children.Add(control);
            s.RunJobs();
            s.Events.Rects.Clear();
            control[Canvas.LeftProperty] = 55;
            s.AssertRects(new Rect(30, 50, 20, 10),
                new Rect(55, 50, 20, 10)
            );
        }
    }

    [Fact]
    public void Control_Should_Invalidate_Child_Rects_When_Moved()
    {
        using (var s = new CompositorCanvas())
        {
            var control = new Decorator()
            {
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
                Padding = new Thickness(10),
                Child = new Border()
                {
                    Width = 20, Height = 10,
                    Background = Brushes.Red
                }
            };
            s.Canvas.Children.Add(control);
            s.RunJobs();
            s.Events.Rects.Clear();
            control[Canvas.LeftProperty] = 55;
            s.AssertRects(new Rect(40, 60, 20, 10),
                new Rect(65, 60, 20, 10)
            );
        }
    }

    [Fact]
    public void Control_Should_Invalidate_Child_Rects_When_Becomes_Invisible()
    {
        using (var s = new CompositorCanvas())
        {
            var control = new Decorator()
            {
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
                Padding = new Thickness(10),
                Child = new Border()
                {
                    Width = 20, Height = 10,
                    Background = Brushes.Red
                }
            };
            s.Canvas.Children.Add(control);
            s.RunJobs();
            s.Events.Rects.Clear();
            control.IsVisible = false;
            s.AssertRects(new Rect(40, 60, 20, 10));
        }
    }

    [Fact]
    public void Old_Child_Rect_Should_Be_Dirty_When_Children_Replaced_Via_VisualChildren()
    {
        using (var s = new CompositorCanvas())
        {
            var host = new LightweightHost
            {
                Width = 200, Height = 200,
                [Canvas.LeftProperty] = 10, [Canvas.TopProperty] = 20
            };
            s.Canvas.Children.Add(host);

            var oldChild = new Border
            {
                Background = Brushes.Red,
                Width = 100, Height = 50
            };
            host.AddChild(oldChild);
            s.RunJobs();
            s.Events.Rects.Clear();

            // Remove old child and add a new child at the same position.
            // The host manages children via VisualChildren directly (no Panel.Children),
            // has no Background, and no Render override — so the compositor must
            // dirty-track the old child's area via ServerList.OnBeforeListClear.
            host.RemoveChild(oldChild);
            var newChild = new Border
            {
                Background = Brushes.Blue,
                Width = 100, Height = 50
            };
            host.AddChild(newChild);

            // The old child's rect (10,20 100x50) must appear in the dirty rects.
            s.RunJobs();
            Assert.Contains(new Rect(10, 20, 100, 50), s.Events.Rects);
        }
    }

    /// <summary>
    /// A lightweight container that manages children directly via
    /// <see cref="Visual.VisualChildren"/> and <see cref="StyledElement.LogicalChildren"/>,
    /// bypassing <see cref="Panel.Children"/>. It has no Background
    /// and no <see cref="Visual.Render"/> override, so the compositor visual never has its
    /// own draw commands.
    /// </summary>
    private class LightweightHost : Control
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in VisualChildren)
            {
                if (child is Layoutable l)
                    l.Measure(availableSize);
            }

            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double y = 0;
            foreach (var child in VisualChildren)
            {
                if (child is Layoutable l)
                {
                    var ds = l.DesiredSize;
                    l.Arrange(new Rect(0, y, ds.Width, ds.Height));
                    y += ds.Height;
                }
            }

            return finalSize;
        }

        public void AddChild(Control child)
        {
            VisualChildren.Add(child);
            LogicalChildren.Add(child);
            InvalidateMeasure();
        }

        public void RemoveChild(Control child)
        {
            VisualChildren.Remove(child);
            LogicalChildren.Remove(child);
            InvalidateMeasure();
        }
    }
}
