using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
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
    public void Changes_To_Child_Of_Invisible_Control_Should_Not_Invalidate()
    {
        using (var s = new CompositorCanvas())
        {
            var child = new Border()
            {
                Width = 20, Height = 10,
                Background = Brushes.Red
            };
            var control = new Decorator()
            {
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
                Padding = new Thickness(10),
                Child = child
            };
            s.Canvas.Children.Add(control);
            s.RunJobs();
            control.IsVisible = false;
            s.RunJobs();
            s.Events.Rects.Clear();
            child.Background = Brushes.Blue;
            s.AssertRects();
        }
    }

    [Fact]
    public void Composition_Translation_Of_Child_Of_Invisible_Control_Should_Not_Invalidate()
    {
        using (var s = new CompositorCanvas())
        {
            var child = new Border()
            {
                Width = 20, Height = 10,
                Background = Brushes.Red
            };
            var control = new Decorator()
            {
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
                Padding = new Thickness(10),
                Child = child
            };
            s.Canvas.Children.Add(control);
            s.RunJobs();
            control.IsVisible = false;
            s.RunJobs();
            s.Events.Rects.Clear();
            ElementComposition.GetElementVisual(child)!.Translation = new Vector3D(5, 0, 0);
            s.AssertRects();
        }
    }

    [Fact]
    public void Control_Should_Invalidate_Updated_Child_Rects_When_Becomes_Visible()
    {
        using (var s = new CompositorCanvas())
        {
            var child = new Border()
            {
                Width = 20, Height = 10,
                Background = Brushes.Red
            };
            var control = new Decorator()
            {
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
                Padding = new Thickness(10),
                Child = child
            };
            s.Canvas.Children.Add(control);
            s.RunJobs();
            control.IsVisible = false;
            s.RunJobs();
            ElementComposition.GetElementVisual(child)!.Translation = new Vector3D(5, 0, 0);
            s.RunJobs();
            s.Events.Rects.Clear();
            control.IsVisible = true;
            s.AssertRects(new Rect(45, 60, 20, 10));
        }
    }
}
