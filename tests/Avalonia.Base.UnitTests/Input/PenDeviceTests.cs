using System;
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

public class PenDeviceTests : PointerTestsBase
{
    [Fact]
    public void Pen_Pointers_Should_Share_Click_Counts()
    {
        using var scope = AvaloniaLocator.EnterScope();
        var settings = new Mock<IPlatformSettings>();
        settings.Setup(x => x.GetDoubleTapTime(PointerType.Pen)).Returns(TimeSpan.FromMilliseconds(500));
        settings.Setup(x => x.GetDoubleTapSize(PointerType.Pen)).Returns(new Size(16, 16));
        AvaloniaLocator.CurrentMutable.Bind<IPlatformSettings>().ToConstant(settings.Object);

        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

        var renderer = new Mock<IHitTester>();
        var device = new PenDevice();
        var impl = CreateTopLevelImplMock();
        var control = new Border();
        var root = CreateInputRoot(impl.Object, control, renderer.Object);
        var clickCounts = new List<int>();

        root.PointerPressed += (_, e) => clickCounts.Add(e.ClickCount);
        SetHit(renderer, control);

        Send(device, root, RawPointerEventType.LeftButtonDown, 1);
        Send(device, root, RawPointerEventType.LeftButtonDown, 2);

        Assert.Equal(new[] { 1, 2 }, clickCounts);
    }

    [Fact]
    public void Pen_Pointers_Should_Have_Separate_Initial_Buttons()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

        var renderer = new Mock<IHitTester>();
        var device = new PenDevice();
        var impl = CreateTopLevelImplMock();
        var control = new Border();
        var root = CreateInputRoot(impl.Object, control, renderer.Object);
        var buttons = new List<MouseButton>();

        root.PointerReleased += (_, e) => buttons.Add(e.InitialPressMouseButton);
        SetHit(renderer, control);

        Send(device, root, RawPointerEventType.LeftButtonDown, 1);
        Send(device, root, RawPointerEventType.RightButtonDown, 2);
        Send(device, root, RawPointerEventType.LeftButtonUp, 1);
        Send(device, root, RawPointerEventType.RightButtonUp, 2);

        Assert.Equal(new[] { MouseButton.Left, MouseButton.Right }, buttons);
    }

    [Fact]
    public void Pen_Pointers_Should_Always_Be_Primary()
    {
        using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

        var renderer = new Mock<IHitTester>();
        var device = new PenDevice();
        var impl = CreateTopLevelImplMock();
        var root = CreateInputRoot(impl.Object, new Border(), renderer.Object);

        var first = Send(device, root, RawPointerEventType.Move, 1);
        var second = Send(device, root, RawPointerEventType.Move, 2);

        Assert.True(device.TryGetPointer(first)!.IsPrimary);
        Assert.True(device.TryGetPointer(second)!.IsPrimary);
    }

    private static RawPointerEventArgs Send(PenDevice device, TopLevel root, RawPointerEventType type, long pointerId)
    {
        var args = CreateRawPointerArgs(device, root, type);
        args.RawPointerId = pointerId;
        root.PlatformImpl!.Input!(args);
        return args;
    }
}
