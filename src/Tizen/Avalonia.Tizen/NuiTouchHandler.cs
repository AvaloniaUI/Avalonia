using Avalonia.Input;
using Avalonia.Input.Raw;
using Tizen.NUI;
using static Tizen.NUI.BaseComponents.View;

namespace Avalonia.Tizen;
internal class NuiTouchHandler
{
    private readonly NuiAvaloniaView _view;
    public TouchDevice _device = new TouchDevice();

    public NuiTouchHandler(NuiAvaloniaView view)
    {
        _view = view;
    }

    private IInputRoot InputRoot => _view.InputRoot;
    private static uint _nextTouchPointId = 1;
    private List<uint> _knownTouches = new();

    public void Handle(TouchEventArgs e)
    {
        var count = e.Touch.GetPointCount();
        for (var i = 0u; i < count; i++)
        {
            uint id;
            if (_knownTouches.Count > i)
            {
                id = _knownTouches[(int)i];
            }
            else
            {
                unchecked
                {
                    id = _nextTouchPointId++;
                }
                _knownTouches.Add(id);
            }

            var point = e.Touch.GetLocalPosition(i);
            var state = e.Touch.GetState(i);
            var timestamp = e.Touch.GetTime();
            var avaloniaState = state switch
            {
                PointStateType.Down => RawPointerEventType.TouchBegin,
                PointStateType.Up => RawPointerEventType.TouchEnd,
                PointStateType.Motion => RawPointerEventType.TouchUpdate,
                PointStateType.Interrupted => RawPointerEventType.TouchCancel,
                _ => RawPointerEventType.TouchUpdate
            };

            var touchEvent = new RawTouchEventArgs(
                _device,
                timestamp,
                InputRoot,
                avaloniaState,
                new Point(point.X, point.Y),
                RawInputModifiers.None,
                id);
            _view.TopLevelImpl.Input?.Invoke(touchEvent);

            if (state is PointStateType.Up or PointStateType.Interrupted)
            {
                _knownTouches.Remove(id);
            }
        }
    }

    public void Handle(WheelEventArgs e)
    {
        var mouseWheelEvent = new RawMouseWheelEventArgs(
            _device,
            e.Wheel.TimeStamp,
            InputRoot,
            new Point(e.Wheel.Point.X, e.Wheel.Point.Y),
            new Vector(
                e.Wheel.Direction == 1 ? e.Wheel.Z : 0, 
                e.Wheel.Direction == 0 ? e.Wheel.Z : 0),
            GetModifierKey(e));

        _view.TopLevelImpl.Input?.Invoke(mouseWheelEvent);
    }

    private RawInputModifiers GetModifierKey(WheelEventArgs ev)
    {
        var modifiers = RawInputModifiers.None;

        if (ev.Wheel.IsShiftModifier())
            modifiers |= RawInputModifiers.Shift;

        if (ev.Wheel.IsAltModifier())
            modifiers |= RawInputModifiers.Alt;

        if (ev.Wheel.IsCtrlModifier())
            modifiers |= RawInputModifiers.Control;

        return modifiers;
    }
}
