using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

public class TitleBarMoveDragHelperTests : ScopedTestBase
{
    private readonly IInputDevice _mouse = new MouseDevice();
    private readonly IInputRoot _root = Mock.Of<IInputRoot>();
    private readonly TitleBarMoveDragHelper _helper = new();

    [Fact]
    public void Move_Within_Threshold_Is_Swallowed()
    {
        UseTapSize(new Size(8, 12));
        var press = BeginPress();

        Assert.True(_helper.HandleEvent(Event(RawPointerEventType.Move, new Point(4, 6)), out var movePress));
        Assert.Null(movePress);
        Assert.True(_helper.HandleEvent(Event(RawPointerEventType.Move, new Point(20, 20)), out movePress));
        Assert.Same(press, movePress);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(0, 7)]
    public void Move_Past_Threshold_Returns_Original_Press_Once(double x, double y)
    {
        UseTapSize(new Size(8, 12));
        var press = BeginPress();

        Assert.True(_helper.HandleEvent(Event(RawPointerEventType.Move, new Point(x, y)), out var movePress));
        Assert.Same(press, movePress);
        Assert.False(_helper.HandleEvent(Event(RawPointerEventType.Move, new Point(20, 20)), out _));
    }

    [Theory]
    [InlineData(RawPointerEventType.LeftButtonUp, true)]
    [InlineData(RawPointerEventType.LeaveWindow, false)]
    [InlineData(RawPointerEventType.CancelCapture, false)]
    [InlineData(RawPointerEventType.RightButtonDown, false)]
    [InlineData(RawPointerEventType.TouchBegin, false)]
    public void Ending_Or_Interrupting_Press_Cancels_Pending_Move(RawPointerEventType type, bool handled)
    {
        BeginPress();

        Assert.Equal(handled, _helper.HandleEvent(Event(type), out var movePress));
        Assert.Null(movePress);
        Assert.False(_helper.HandleEvent(Event(RawPointerEventType.Move, new Point(20, 20)), out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public void Hover_After_Missing_Release_Cancels_And_Reaches_Client(double x)
    {
        BeginPress();
        var hover = Event(RawPointerEventType.Move, new Point(x, 0));
        hover.InputModifiers = RawInputModifiers.None;

        Assert.False(_helper.HandleEvent(hover, out var movePress));
        Assert.Null(movePress);
        Assert.False(_helper.HandleEvent(Event(RawPointerEventType.Move, new Point(20, 20)), out _));
    }

    private static void UseTapSize(Size size)
    {
        var settings = new Mock<IPlatformSettings>();
        settings.Setup(s => s.GetTapSize(PointerType.Mouse)).Returns(size);
        AvaloniaLocator.CurrentMutable.Bind<IPlatformSettings>().ToConstant(settings.Object);
    }

    private RawPointerEventArgs BeginPress()
    {
        var press = Event(RawPointerEventType.LeftButtonDown);
        _helper.Begin(press);
        return press;
    }

    private RawPointerEventArgs Event(RawPointerEventType type, Point position = default) =>
        new(_mouse, 100, _root, type, position, RawInputModifiers.LeftMouseButton);
}
