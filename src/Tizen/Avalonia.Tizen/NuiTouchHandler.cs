using System.Threading;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Threading;
using Tizen.NUI;
using static Tizen.NUI.BaseComponents.View;

namespace Avalonia.Tizen;
internal class NuiTouchHandler
{
    private readonly NuiAvaloniaView _view;
    private readonly ITopLevelImpl _topLevelImpl;
    public TouchDevice _device = new TouchDevice();

    public NuiTouchHandler(NuiAvaloniaView view, ITopLevelImpl tl)
    {
        _view = view;
        _topLevelImpl = tl;
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
            var mouseEvent = new RawTouchEventArgs(
                _device,
                timestamp,
                InputRoot,
                state switch
                {
                    PointStateType.Down => RawPointerEventType.TouchBegin,
                    PointStateType.Up => RawPointerEventType.TouchEnd,
                    PointStateType.Motion => RawPointerEventType.Move,
                    PointStateType.Interrupted => RawPointerEventType.TouchCancel,
                    _ => RawPointerEventType.Move
                },
                new Point(point.X, point.Y),
                RawInputModifiers.None,
                id);
            _topLevelImpl.Input?.Invoke(mouseEvent);

            if (state is PointStateType.Up or PointStateType.Interrupted)
            {
                _knownTouches.Remove(id);
            }
        }
    }
}
