using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input;

public class KeyboardNavigationTests_XY : ScopedTestBase
{
    private static (Canvas canvas, Button[] buttons) CreateXYTestLayout()
    {
        //  111
        //  111
        //  111
        //         2
        // 3
        //
        //   4
        Button x1, x2, x3, x4;
        var canvas = new Canvas
        {
            Width = 500,
            Children =
            {
                (x1 = new Button
                {
                    Content = "A",
                    [Canvas.LeftProperty] = 50, [Canvas.TopProperty] = 0, Width = 150, Height = 150,
                }),
                (x2 = new Button
                {
                    Content = "B",
                    [Canvas.LeftProperty] = 400, [Canvas.TopProperty] = 150, Width = 50, Height = 50,
                }),
                (x3 = new Button
                {
                    Content = "C",
                    [Canvas.LeftProperty] = 0, [Canvas.TopProperty] = 200, Width = 50, Height = 50,
                }),
                (x4 = new Button
                {
                    Content = "D",
                    [Canvas.LeftProperty] = 100, [Canvas.TopProperty] = 300, Width = 50, Height = 50,
                })
            }
        };

        return (canvas, new[] { x1, x2, x3, x4 });
    }

    [Theory]
    [InlineData(1, NavigationDirection.Down, 4)]
    [InlineData(1, NavigationDirection.Up, -1)]
    [InlineData(1, NavigationDirection.Left, -1)]
    [InlineData(1, NavigationDirection.Right, 2)]
    // TODO: [InlineData(2, NavigationDirection.Down, 4)] Actual: 3
    // TODO: [InlineData(2, NavigationDirection.Up, -1)] Actual 1
    [InlineData(2, NavigationDirection.Left, 1)]
    [InlineData(2, NavigationDirection.Right, -1)]
    [InlineData(3, NavigationDirection.Down, 4)]
    // TODO: [InlineData(3, NavigationDirection.Up, 1)] Actual: 2
    [InlineData(3, NavigationDirection.Left, -1)]
    // TODO: [InlineData(3, NavigationDirection.Right, 4)] Actual: 1
    [InlineData(4, NavigationDirection.Down, -1)]
    [InlineData(4, NavigationDirection.Up, 1)]
    [InlineData(4, NavigationDirection.Left, 3)]
    [InlineData(4, NavigationDirection.Right, 2)]
    public void Projection_Focus_Depending_On_Direction(int from, NavigationDirection direction, int to)
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);
        
        var (canvas, buttons) = CreateXYTestLayout();
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = canvas
        };
        window.Show();

        var fromButton = buttons[from - 1];
        fromButton.SetValue(XYFocus.UpNavigationStrategyProperty, XYFocusNavigationStrategy.Projection);
        fromButton.SetValue(XYFocus.LeftNavigationStrategyProperty, XYFocusNavigationStrategy.Projection);
        fromButton.SetValue(XYFocus.RightNavigationStrategyProperty, XYFocusNavigationStrategy.Projection);
        fromButton.SetValue(XYFocus.DownNavigationStrategyProperty, XYFocusNavigationStrategy.Projection);

        var result = KeyboardNavigationHandler.GetNext(fromButton, direction) as Button;

        Assert.Equal(to, result == null ? -1 : Array.IndexOf(buttons, result) + 1);
    }
    
    [Theory]
    [InlineData(1, NavigationDirection.Down, 3)]
    [InlineData(1, NavigationDirection.Up, -1)]
    [InlineData(1, NavigationDirection.Left, 3)]
    [InlineData(1, NavigationDirection.Right, 2)]
    [InlineData(2, NavigationDirection.Down, 3)]
    [InlineData(2, NavigationDirection.Up, 1)]
    [InlineData(2, NavigationDirection.Left, 1)]
    [InlineData(2, NavigationDirection.Right, -1)]
    [InlineData(3, NavigationDirection.Down, 4)]
    [InlineData(3, NavigationDirection.Up, 1)]
    [InlineData(3, NavigationDirection.Left, -1)]
    [InlineData(3, NavigationDirection.Right, 1)]
    [InlineData(4, NavigationDirection.Down, -1)]
    [InlineData(4, NavigationDirection.Up, 3)]
    [InlineData(4, NavigationDirection.Left, 3)]
    [InlineData(4, NavigationDirection.Right, 2)]
    public void RectilinearDistance_Focus_Depending_On_Direction(int from, NavigationDirection direction, int to)
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);
        
        var (canvas, buttons) = CreateXYTestLayout();
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = canvas
        };
        window.Show();

        var fromButton = buttons[from - 1];
        fromButton.SetValue(XYFocus.UpNavigationStrategyProperty, XYFocusNavigationStrategy.RectilinearDistance);
        fromButton.SetValue(XYFocus.LeftNavigationStrategyProperty, XYFocusNavigationStrategy.RectilinearDistance);
        fromButton.SetValue(XYFocus.RightNavigationStrategyProperty, XYFocusNavigationStrategy.RectilinearDistance);
        fromButton.SetValue(XYFocus.DownNavigationStrategyProperty, XYFocusNavigationStrategy.RectilinearDistance);

        var result = KeyboardNavigationHandler.GetNext(fromButton, direction) as Button;

        Assert.Equal(to, result == null ? -1 : Array.IndexOf(buttons, result) + 1);
    }
    
    [Theory]
    [InlineData(1, NavigationDirection.Down, 2)]
    [InlineData(1, NavigationDirection.Up, -1)]
    [InlineData(1, NavigationDirection.Left, 3)]
    [InlineData(1, NavigationDirection.Right, 2)]
    [InlineData(2, NavigationDirection.Down, 3)]
    [InlineData(2, NavigationDirection.Up, 1)]
    [InlineData(2, NavigationDirection.Left, 1)]
    [InlineData(2, NavigationDirection.Right, -1)]
    [InlineData(3, NavigationDirection.Down, 4)]
    [InlineData(3, NavigationDirection.Up, 2)]
    [InlineData(3, NavigationDirection.Left, -1)]
    [InlineData(3, NavigationDirection.Right, 1)]
    [InlineData(4, NavigationDirection.Down, -1)]
    [InlineData(4, NavigationDirection.Up, 3)]
    [InlineData(4, NavigationDirection.Left, 3)]
    [InlineData(4, NavigationDirection.Right, 2)]
    public void NavigationDirectionDistance_Focus_Depending_On_Direction(int from, NavigationDirection direction, int to)
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);
        
        var (canvas, buttons) = CreateXYTestLayout();
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = canvas
        };
        window.Show();

        var fromButton = buttons[from - 1];
        fromButton.SetValue(XYFocus.UpNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);
        fromButton.SetValue(XYFocus.LeftNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);
        fromButton.SetValue(XYFocus.RightNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);
        fromButton.SetValue(XYFocus.DownNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);

        var result = KeyboardNavigationHandler.GetNext(fromButton, direction) as Button;

        Assert.Equal(to, result == null ? -1 : Array.IndexOf(buttons, result) + 1);
    }
    
    [Fact]
    public void Uses_XY_Directional_Overrides()
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

        var left = new Button();
        var right = new Button();
        var up = new Button();
        var down = new Button();
        var center = new Button
        {
            [XYFocus.LeftProperty] = left,
            [XYFocus.RightProperty] = right,
            [XYFocus.UpProperty] = up,
            [XYFocus.DownProperty] = down,
        };
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = new Canvas
            {
                Children =
                {
                    left, right, up, down, center
                }
            }
        };
        window.Show();
        
        Assert.Equal(left, KeyboardNavigationHandler.GetNext(center, NavigationDirection.Left));
        Assert.Equal(right, KeyboardNavigationHandler.GetNext(center, NavigationDirection.Right));
        Assert.Equal(up, KeyboardNavigationHandler.GetNext(center, NavigationDirection.Up));
        Assert.Equal(down, KeyboardNavigationHandler.GetNext(center, NavigationDirection.Down));
    }
    
    [Fact]
    public void XY_Directional_Override_Discarded_If_Not_Part_Of_The_Same_Root()
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

        var left = new Button();
        var center = new Button
        {
            [XYFocus.LeftProperty] = left
        };
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = center
        };
        window.Show();
        
        Assert.Null(KeyboardNavigationHandler.GetNext(center, NavigationDirection.Left));
    }

    [Fact]
    public void Parent_Can_Override_Navigation_When_Directional_Is_Set()
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

        // With double stack panel layout we have something like this:
        // [ [ EXPECTED, CURRENT ] CANDIDATE ]
        // Where normally from Current focus would go to the Candidate.
        // But since we set `XYFocus.Right` on nested StackPanel, it should be used instead.
        // But ONLY if Candidate isn't part of that nested StackPanel (it isn't). 

        var current = new Button();
        var candidate = new Button();
        var expectedOverride = new Button();
        var parent = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { expectedOverride, current },
            [XYFocus.RightProperty] = expectedOverride,
            // Property value to simplify test.
            [XYFocus.RightNavigationStrategyProperty] = XYFocusNavigationStrategy.RectilinearDistance
        };
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { parent, candidate }
            }
        };
        window.Show();

        Assert.Equal(expectedOverride, KeyboardNavigationHandler.GetNext(current, NavigationDirection.Right));
    }
    
    [Fact]
    public void Clipped_Element_Should_Not_Be_Focused()
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

        var current = new Button() { Height = 20 };
        var candidate = new Button() { Height = 20 };
        var parent = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 20,
            Children = { current, candidate }
        };
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = parent,
            Height = 30
        };
        window.Show();

        Assert.Null(KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down));
    }
    
    [Fact]
    public void Clipped_Element_Should_Not_Focused_If_Inside_Of_ScrollViewer()
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);
        
        var current = new Button() { Height = 20 };
        var candidate = new Button() { Height = 20 };
        var parent = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 20,
            Children = { current, candidate }
        };
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = new ScrollViewer
            {
                Content = parent
            },
            Height = 30
        };
        window.Show();

        Assert.Equal(candidate, KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down));
    }

    [Theory]
    [InlineData(Key.Left, NavigationDirection.Left)]
    [InlineData(Key.Right, NavigationDirection.Right)]
    [InlineData(Key.Up, NavigationDirection.Up)]
    [InlineData(Key.Down, NavigationDirection.Down)]
    public void Arrow_Key_Should_Focus_Element(Key key, NavigationDirection direction)
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);
        
        var candidate = new Button();
        var current = new Button();
        current[direction switch
        {
            NavigationDirection.Left => XYFocus.LeftProperty,
            NavigationDirection.Right => XYFocus.RightProperty,
            NavigationDirection.Up => XYFocus.UpProperty,
            NavigationDirection.Down => XYFocus.DownProperty,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        }] = candidate;
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = new Canvas
            {
                Children = { current, candidate }
            }
        };
        window.Show();
        Assert.True(current.Focus());

        var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = current };
        window.RaiseEvent(args);
        
        Assert.Equal(candidate, FocusManager.GetFocusManager(current)!.GetFocusedElement());
        Assert.True(args.Handled);
    }
    
    [Theory]
    [InlineData(Key.Left, NavigationDirection.Left)]
    [InlineData(Key.Right, NavigationDirection.Right)]
    [InlineData(Key.Up, NavigationDirection.Up)]
    [InlineData(Key.Down, NavigationDirection.Down)]
    public void Arrow_Key_Should_Not_Be_Handled_If_No_Focus(Key key, NavigationDirection direction)
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);
        
        var current = new Button();
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = new Canvas
            {
                Children = { current }
            }
        };
        window.Show();
        Assert.True(current.Focus());

        var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = current };
        window.RaiseEvent(args);
        
        Assert.Equal(current, FocusManager.GetFocusManager(current)!.GetFocusedElement());
        Assert.False(args.Handled);
    }

    [Fact]
    public void Can_Focus_Child_Of_Current_Focused()
    {
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

        var candidate = new Button() { Height = 20, Width = 20 };
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = candidate,
            Height = 30
        };
        window.Show();

        Assert.Null(KeyboardNavigationHandler.GetNext(window, NavigationDirection.Down));
    }

    [Fact]
    public void Can_Focus_Any_Element_If_Nothing_Was_Focused()
    {
        // In the future we might auto-focus any element, but for now XY algorithm should be aware of Avalonia specifics.
        using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

        var candidate = new Button();
        var window = new Window
        {
            [XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
            Content = new Canvas
            {
                Children = { candidate }
            }
        };
        window.Show();

        Assert.Null(FocusManager.GetFocusManager(window)!.GetFocusedElement());

        var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down, Source = window };
        window.RaiseEvent(args);

        Assert.Equal(candidate, FocusManager.GetFocusManager(window)!.GetFocusedElement());
    }
}
