using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input;

public class KeyboardNavigationTests_XY
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
                    [Canvas.LeftProperty] = 50, [Canvas.TopProperty] = 0, Width = 150, Height = 150,
                }),
                (x2 = new Button
                {
                    [Canvas.LeftProperty] = 400, [Canvas.TopProperty] = 150, Width = 50, Height = 50,
                }),
                (x3 = new Button
                {
                    [Canvas.LeftProperty] = 0, [Canvas.TopProperty] = 200, Width = 50, Height = 50,
                }),
                (x4 = new Button
                {
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
    [InlineData(2, NavigationDirection.Down, 3)]
    [InlineData(2, NavigationDirection.Up, 1)]
    [InlineData(2, NavigationDirection.Left, 1)]
    [InlineData(2, NavigationDirection.Right, -1)]
    [InlineData(3, NavigationDirection.Down, 4)]
    [InlineData(3, NavigationDirection.Up, 2)]
    [InlineData(3, NavigationDirection.Left, -1)]
    [InlineData(3, NavigationDirection.Right, 1)]
    [InlineData(4, NavigationDirection.Down, -1)]
    [InlineData(4, NavigationDirection.Up, 1)]
    [InlineData(4, NavigationDirection.Left, 3)]
    [InlineData(4, NavigationDirection.Right, 2)]
    public void Projection_Focus_Depending_On_Direction(int from, NavigationDirection direction, int to)
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
        
        var (canvas, buttons) = CreateXYTestLayout();
        var window = new Window { Content = canvas };
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
    [InlineData(1, NavigationDirection.Left, -1)]
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
    [InlineData(4, NavigationDirection.Up, 1)]
    [InlineData(4, NavigationDirection.Left, 3)]
    [InlineData(4, NavigationDirection.Right, 2)]
    public void RectilinearDistance_Focus_Depending_On_Direction(int from, NavigationDirection direction, int to)
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
        
        var (canvas, buttons) = CreateXYTestLayout();
        var window = new Window { Content = canvas };
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
    [InlineData(1, NavigationDirection.Left, -1)]
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
    [InlineData(4, NavigationDirection.Up, 1)]
    [InlineData(4, NavigationDirection.Left, 3)]
    [InlineData(4, NavigationDirection.Right, 2)]
    public void NavigationDirectionDistance_Focus_Depending_On_Direction(int from, NavigationDirection direction, int to)
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
        
        var (canvas, buttons) = CreateXYTestLayout();
        var window = new Window { Content = canvas };
        window.Show();

        var fromButton = buttons[from - 1];
        fromButton.SetValue(XYFocus.UpNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);
        fromButton.SetValue(XYFocus.LeftNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);
        fromButton.SetValue(XYFocus.RightNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);
        fromButton.SetValue(XYFocus.DownNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);

        var result = KeyboardNavigationHandler.GetNext(fromButton, direction) as Button;

        Assert.Equal(to, result == null ? -1 : Array.IndexOf(buttons, result) + 1);
    }
}
