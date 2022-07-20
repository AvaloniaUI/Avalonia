using Avalonia.Controls;
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
}