using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Input;

[InvariantCulture]
public partial class MouseDeviceTests
{
    [Theory]
    [InlineData(RawPointerEventType.Magnify, RawPointerGesturePhase.Completed)]
    [InlineData(RawPointerEventType.Magnify, RawPointerGesturePhase.Canceled)]
    [InlineData(RawPointerEventType.Rotate, RawPointerGesturePhase.Completed)]
    [InlineData(RawPointerEventType.Rotate, RawPointerGesturePhase.Canceled)]
    [InlineData(RawPointerEventType.Swipe, RawPointerGesturePhase.Completed)]
    [InlineData(RawPointerEventType.Swipe, RawPointerGesturePhase.Canceled)]
    public void TouchPad_Gesture_Events_Are_Raised_On_The_Element_That_Started_The_Gesture(
        RawPointerEventType type,
        RawPointerGesturePhase endPhase)
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();
        var name = type.ToString();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Started));
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Updated, 0.5));
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, endPhase));

        // Once the gesture has ended, events are hit tested again.
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.None, 0.25));

        var isCanceled = endPhase == RawPointerGesturePhase.Canceled;
        Assert.Equal(
            [
                $"{name}Started a", $"{name} a 0.5",
                $"{name}Ended a canceled={isCanceled}",
                $"{name} b 0.25"
            ],
            log);
    }

    [Theory]
    [InlineData(RawPointerEventType.Magnify)]
    [InlineData(RawPointerEventType.Rotate)]
    [InlineData(RawPointerEventType.Swipe)]
    public void TouchPad_Gesture_Raises_Delta_Events_For_Began_And_Ended_Phases_With_Delta(RawPointerEventType type)
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, _, log) = CreateGestureTestRoot();
        var device = new MouseDevice();
        var name = type.ToString();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Started, 0.1));
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Completed, 0.2));

        Assert.Equal(
            [
                $"{name}Started a",
                $"{name} a 0.1",
                $"{name} a 0.2",
                $"{name}Ended a canceled=False"
            ],
            log);
    }

    [Theory]
    [InlineData(RawPointerEventType.Magnify)]
    [InlineData(RawPointerEventType.Rotate)]
    [InlineData(RawPointerEventType.Swipe)]
    public void TouchPad_Gesture_Ended_Is_Not_Raised_Without_Started(RawPointerEventType type)
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, _, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Canceled));

        Assert.Empty(log);
    }

    [Theory]
    [InlineData(RawPointerEventType.Magnify)]
    [InlineData(RawPointerEventType.Rotate)]
    [InlineData(RawPointerEventType.Swipe)]
    public void TouchPad_Gesture_Began_Cancels_The_Unfinished_Previous_Gesture(RawPointerEventType type)
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();
        var name = type.ToString();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Started));
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Started));

        Assert.Equal(
            [
                $"{name}Started a",
                $"{name}Ended a canceled=True",
                $"{name}Started b"
            ],
            log);
    }

    [Fact]
    public void TouchPad_Gestures_Are_Latched_Independently()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawGestureArgs(device, root, RawPointerEventType.Magnify, RawPointerGesturePhase.Started));
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawGestureArgs(device, root, RawPointerEventType.Rotate, RawPointerGesturePhase.Started));
        impl.Object.Input!(CreateRawGestureArgs(device, root, RawPointerEventType.Magnify, RawPointerGesturePhase.Updated, 0.5));
        impl.Object.Input!(CreateRawGestureArgs(device, root, RawPointerEventType.Rotate, RawPointerGesturePhase.Updated, 0.5));

        Assert.Equal(
            [
                "MagnifyStarted a",
                "RotateStarted b",
                "Magnify a 0.5",
                "Rotate b 0.5"
            ],
            log);
    }

    [Fact]
    public void TouchPad_Swipe_Is_Raised_On_The_Element_Under_The_Pointer()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawGestureArgs(device, root, RawPointerEventType.Swipe, RawPointerGesturePhase.None, 1));
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawGestureArgs(device, root, RawPointerEventType.Swipe, RawPointerGesturePhase.None, -1));

        Assert.Equal(
            [
                "Swipe a 1",
                "Swipe b -1"],
            log);
    }

    [Fact]
    public void Wheel_Gesture_And_Its_Inertia_Are_Raised_On_The_Element_That_Started_The_Gesture()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Started));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Updated, 1));

        // The content scrolled under the pointer.
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Updated, 2));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Completed));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Started, 3, isInertial: true));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Updated, 4, isInertial: true));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Completed, isInertial: true));

        // Once the gesture has ended, events are hit tested again.
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.None, 5));

        Assert.Equal(
            [
                "WheelStarted a inertial=False",
                "Wheel a 1 inertial=False",
                "Wheel a 2 inertial=False",
                "WheelEnded a inertial=False canceled=False",
                "WheelStarted a inertial=True",
                "Wheel a 3 inertial=True",
                "Wheel a 4 inertial=True",
                "WheelEnded a inertial=True canceled=False",
                "Wheel b 5 inertial=False"
            ],
            log);
    }

    [Fact]
    public void Wheel_Inertia_Is_Hit_Tested_When_Another_Wheel_Event_Comes_First()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Started));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Completed));
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.None, 1));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Started, 2, isInertial: true));

        Assert.Equal(
            [
                "WheelStarted a inertial=False",
                "WheelEnded a inertial=False canceled=False",
                "Wheel b 1 inertial=False",
                "WheelStarted b inertial=True",
                "Wheel b 2 inertial=True"
            ],
            log);
    }

    [Fact]
    public void Wheel_Phase_Events_Without_Delta_Dont_Raise_PointerWheelChanged()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, _, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Started));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Updated));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Canceled));

        Assert.Equal(["WheelStarted a inertial=False", "WheelEnded a inertial=False canceled=True"], log);
    }

    [Fact]
    public void Wheel_Events_Without_Phase_Are_Not_Latched()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.None, 1));
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.None, 2));

        Assert.Equal(
            [
                "Wheel a 1 inertial=False",
                "Wheel b 2 inertial=False"
            ],
            log);
    }

    [Theory]
    [InlineData(RawPointerEventType.Wheel)]
    [InlineData(RawPointerEventType.Magnify)]
    [InlineData(RawPointerEventType.Rotate)]
    [InlineData(RawPointerEventType.Swipe)]
    public void Events_Without_Phase_And_Delta_Are_Not_Raised(RawPointerEventType type)
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, _, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(type == RawPointerEventType.Wheel ?
            CreateRawWheelArgs(device, root, RawPointerGesturePhase.None) :
            CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.None));

        Assert.Empty(log);
    }

    [Fact]
    public void Wheel_Events_Without_Phase_Are_Raised_On_The_Element_Of_The_Active_Gesture()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Started));
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.None, 1));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Completed));

        Assert.Equal(
            [
                "WheelStarted a inertial=False",
                "Wheel a 1 inertial=False",
                "WheelEnded a inertial=False canceled=False"
            ],
            log);
    }

    [Theory]
    [InlineData(RawPointerEventType.Magnify)]
    [InlineData(RawPointerEventType.Rotate)]
    [InlineData(RawPointerEventType.Swipe)]
    public void TouchPad_Gesture_Updates_Without_Delta_Are_Not_Raised(RawPointerEventType type)
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, _, log) = CreateGestureTestRoot();
        var device = new MouseDevice();
        var name = type.ToString();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Started));
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Updated));
        impl.Object.Input!(CreateRawGestureArgs(device, root, type, RawPointerGesturePhase.Completed));

        Assert.Equal(
            [
                $"{name}Started a",
                $"{name}Ended a canceled=False"
            ],
            log);
    }

    [Fact]
    public void Wheel_Gesture_Falls_Back_To_Hit_Testing_When_Latched_Element_Is_Removed()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Started));
        ((Panel)a.Parent!).Children.Remove(a);
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Updated, 1));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Completed));

        Assert.Equal(
            [
                "WheelStarted a inertial=False",
                "Wheel b 1 inertial=False"
            ],
            log);
    }

    [Fact]
    public void Platform_Capture_Loss_Cancels_Active_Gestures()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));
        var (impl, renderer, root, a, b, log) = CreateGestureTestRoot();
        var device = new MouseDevice();

        SetHit(renderer, a);
        impl.Object.Input!(CreateRawGestureArgs(device, root, RawPointerEventType.Magnify, RawPointerGesturePhase.Started));
        SetHit(renderer, b);
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Started));
        impl.Object.Input!(CreateRawPointerArgs(device, root, RawPointerEventType.CancelCapture));

        // The platform's own end events are ignored, as there's no gesture left to end.
        impl.Object.Input!(CreateRawGestureArgs(device, root, RawPointerEventType.Magnify, RawPointerGesturePhase.Completed));
        impl.Object.Input!(CreateRawWheelArgs(device, root, RawPointerGesturePhase.Completed));

        Assert.Equal(
            [
                "MagnifyStarted a",
                "WheelStarted b inertial=False",
                "MagnifyEnded a canceled=True",
                "WheelEnded b inertial=False canceled=True"
            ],
            log);
    }

    private static (Mock<IWindowImpl> impl, Mock<IHitTester> renderer, TopLevel root, Control a, Control b, List<string> log)
        CreateGestureTestRoot()
    {
        var renderer = new Mock<IHitTester>();
        var impl = CreateTopLevelImplMock();
        var a = new Control { Name = "a" };
        var b = new Control { Name = "b" };
        var root = CreateInputRoot(impl.Object, new StackPanel { Children = { a, b } }, renderer.Object);
        var log = new List<string>();

        static string Name(object? source) => (source as Control)?.Name ?? "?";

        var touchpadGestures = new[]
        {
            (
                "Magnify",
                InputElement.PointerTouchPadGestureMagnifyStartedEvent,
                InputElement.PointerTouchPadGestureMagnifyEvent,
                InputElement.PointerTouchPadGestureMagnifyEndedEvent
            ),
            (
                "Rotate",
                InputElement.PointerTouchPadGestureRotateStartedEvent,
                InputElement.PointerTouchPadGestureRotateEvent,
                InputElement.PointerTouchPadGestureRotateEndedEvent
            ),
            (
                "Swipe",
                InputElement.PointerTouchPadGestureSwipeStartedEvent,
                InputElement.PointerTouchPadGestureSwipeEvent,
                InputElement.PointerTouchPadGestureSwipeEndedEvent
            )
        };

        foreach (var (gesture, started, delta, ended) in touchpadGestures)
        {
            root.AddHandler(started, (_, e) => log.Add($"{gesture}Started {Name(e.Source)}"));
            root.AddHandler(delta, (_, e) => log.Add($"{gesture} {Name(e.Source)} {e.Delta.X}"));
            root.AddHandler(ended, (_, e) => log.Add($"{gesture}Ended {Name(e.Source)} canceled={e.IsCanceled}"));
        }

        root.AddHandler(
            InputElement.PointerWheelGestureStartedEvent,
            (_, e) => log.Add($"WheelStarted {Name(e.Source)} inertial={e.IsInertial}"));
        root.AddHandler(
            InputElement.PointerWheelChangedEvent,
            (_, e) => log.Add($"Wheel {Name(e.Source)} {e.Delta.Y} inertial={e.IsInertial}"));
        root.AddHandler(
            InputElement.PointerWheelGestureEndedEvent,
            (_, e) => log.Add($"WheelEnded {Name(e.Source)} inertial={e.IsInertial} canceled={e.IsCanceled}"));

        return (impl, renderer, root, a, b, log);
    }

    private static RawPointerGestureEventArgs CreateRawGestureArgs(
        MouseDevice device,
        TopLevel root,
        RawPointerEventType type,
        RawPointerGesturePhase phase,
        double delta = 0)
        => new(device, 0, root.PresentationSource, type, default, new Vector(delta, delta), default)
        {
            Phase = phase
        };

    private static RawMouseWheelEventArgs CreateRawWheelArgs(
        MouseDevice device,
        TopLevel root,
        RawPointerGesturePhase phase,
        double deltaY = 0,
        bool isInertial = false)
        => new(device, 0, root.PresentationSource, default, new Vector(0, deltaY), default)
        {
            Phase = phase,
            IsInertial = isInertial
        };
}
