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
    public void Sibling_Controls_Should_Invalidate_Union_Rect_When_Removed()
    {
        using (var s = new CompositorCanvas())
        {
            var control = new Border()
            {
                Background = Brushes.Red, Width = 20, Height = 10,
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 10
            };
            var control2 = new Border()
            {
                Background = Brushes.Blue, Width = 20, Height = 10,
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50
            };
            s.Canvas.Children.Add(control);
            s.Canvas.Children.Add(control2);
            s.RunJobs();
            s.Events.Rects.Clear();
            s.Canvas.Children.Remove(control);
            s.Canvas.Children.Remove(control2);
            s.AssertRects(new Rect(30, 10, 20, 50));
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
    public void Sibling_Controls_Should_Invalidate_Rects_When_ZIndex_Order_Changes()
    {
        using (var s = new CompositorCanvas())
        {
            // The siblings draw nothing themselves, so only their reordering can invalidate anything.
            var back = new Decorator
            {
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
                ZIndex = 1,
                Child = new Border { Width = 20, Height = 10, Background = Brushes.Red }
            };
            var front = new Decorator
            {
                [Canvas.LeftProperty] = 40, [Canvas.TopProperty] = 55,
                ZIndex = 2,
                Child = new Border { Width = 20, Height = 10, Background = Brushes.Blue }
            };
            s.Canvas.Children.Add(back);
            s.Canvas.Children.Add(front);
            s.RunJobs();
            s.Events.Rects.Clear();

            back.ZIndex = 2;
            front.ZIndex = 1;

            s.AssertRects(new Rect(30, 50, 20, 10), new Rect(40, 55, 20, 10));
        }
    }

    [Fact]
    public void Control_Should_Not_Invalidate_Sibling_Rects_When_Inserted_Below()
    {
        using (var s = new CompositorCanvas())
        {
            for (var c = 0; c < 2; c++)
            {
                s.Canvas.Children.Add(new Border
                {
                    Background = Brushes.Red, Width = 20, Height = 10,
                    [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50 + c * 20
                });
            }
            s.RunJobs();
            s.Events.Rects.Clear();

            s.Canvas.Children.Insert(0, new Border
            {
                Background = Brushes.Blue, Width = 20, Height = 10,
                [Canvas.LeftProperty] = 100, [Canvas.TopProperty] = 50
            });

            s.AssertRects(new Rect(100, 50, 20, 10));
        }
    }
}
