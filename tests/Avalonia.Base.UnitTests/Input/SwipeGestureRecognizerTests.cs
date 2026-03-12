using System.Collections.Generic;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input;

public class SwipeGestureRecognizerTests
{
    [Fact]
    public void Does_Not_Raise_Swipe_When_Both_Axes_Are_Disabled()
    {
        var (border, root) = CreateTarget(new SwipeGestureRecognizer { Threshold = 1 });
        var touch = new TouchTestHelper();
        var swipeRaised = false;
        var endedRaised = false;

        root.AddHandler(InputElement.SwipeGestureEvent, (_, _) => swipeRaised = true);
        root.AddHandler(InputElement.SwipeGestureEndedEvent, (_, _) => endedRaised = true);

        touch.Down(border, new Point(50, 50));
        touch.Move(border, new Point(20, 20));
        touch.Up(border, new Point(20, 20));

        Assert.False(swipeRaised);
        Assert.False(endedRaised);
    }

    [Fact]
    public void Defaults_Disable_Both_Axes()
    {
        var recognizer = new SwipeGestureRecognizer();

        Assert.False(recognizer.CanHorizontallySwipe);
        Assert.False(recognizer.CanVerticallySwipe);
    }

    [Fact]
    public void Starts_Only_After_Threshold_Is_Exceeded()
    {
        var (border, root) = CreateTarget(new SwipeGestureRecognizer
        {
            CanHorizontallySwipe = true,
            Threshold = 50
        });
        var touch = new TouchTestHelper();
        var deltas = new List<Vector>();

        root.AddHandler(InputElement.SwipeGestureEvent, (_, e) => deltas.Add(e.Delta));

        touch.Down(border, new Point(5, 5));
        touch.Move(border, new Point(40, 5));

        Assert.Empty(deltas);

        touch.Move(border, new Point(80, 5));

        Assert.Single(deltas);
        Assert.NotEqual(Vector.Zero, deltas[0]);
    }

    [Fact]
    public void Ended_Event_Uses_Same_Id_And_Last_Velocity()
    {
        var (border, root) = CreateTarget(new SwipeGestureRecognizer
        {
            CanHorizontallySwipe = true,
            Threshold = 1
        });
        var touch = new TouchTestHelper();
        var updateIds = new List<int>();
        var velocities = new List<Vector>();
        var endedId = 0;
        var endedVelocity = Vector.Zero;

        root.AddHandler(InputElement.SwipeGestureEvent, (_, e) =>
        {
            updateIds.Add(e.Id);
            velocities.Add(e.Velocity);
        });
        root.AddHandler(InputElement.SwipeGestureEndedEvent, (_, e) =>
        {
            endedId = e.Id;
            endedVelocity = e.Velocity;
        });

        touch.Down(border, new Point(50, 50));
        touch.Move(border, new Point(40, 50));
        Thread.Sleep(10);
        touch.Move(border, new Point(30, 50));
        touch.Up(border, new Point(30, 50));

        Assert.True(updateIds.Count >= 2);
        Assert.All(updateIds, id => Assert.Equal(updateIds[0], id));
        Assert.Equal(updateIds[0], endedId);
        Assert.NotEqual(Vector.Zero, velocities[^1]);
        Assert.Equal(velocities[^1], endedVelocity);
    }

    [Fact]
    public void Mouse_Swipe_Requires_IsMouseEnabled()
    {
        var mouse = new MouseTestHelper();
        var (border, root) = CreateTarget(new SwipeGestureRecognizer
        {
            CanHorizontallySwipe = true,
            Threshold = 1
        });
        var swipeRaised = false;

        root.AddHandler(InputElement.SwipeGestureEvent, (_, _) => swipeRaised = true);

        mouse.Down(border, position: new Point(50, 50));
        mouse.Move(border, new Point(30, 50));
        mouse.Up(border, position: new Point(30, 50));

        Assert.False(swipeRaised);
    }

    [Fact]
    public void Mouse_Swipe_Is_Raised_When_Enabled()
    {
        var mouse = new MouseTestHelper();
        var (border, root) = CreateTarget(new SwipeGestureRecognizer
        {
            CanHorizontallySwipe = true,
            Threshold = 1,
            IsMouseEnabled = true
        });
        var swipeRaised = false;

        root.AddHandler(InputElement.SwipeGestureEvent, (_, _) => swipeRaised = true);

        mouse.Down(border, position: new Point(50, 50));
        mouse.Move(border, new Point(30, 50));
        mouse.Up(border, position: new Point(30, 50));

        Assert.True(swipeRaised);
    }

    private static (Border Border, TestRoot Root) CreateTarget(SwipeGestureRecognizer recognizer)
    {
        var border = new Border
        {
            Width = 100,
            Height = 100
        };
        border.GestureRecognizers.Add(recognizer);

        var root = new TestRoot
        {
            Child = border
        };

        return (border, root);
    }
}
