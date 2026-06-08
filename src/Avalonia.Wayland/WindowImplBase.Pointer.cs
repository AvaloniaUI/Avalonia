using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland;

partial class WindowBaseImpl
{
    partial class Sink
    {
        void IWSurfaceEventSink.OnPointerEnter(ulong timestamp, uint serial, Point position)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawPointerEventArgs(_mouse, timestamp, _inputRoot,
                RawPointerEventType.Move, position, RawInputModifiers.None));
        }

        void IWSurfaceEventSink.OnPointerLeave(uint serial)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawPointerEventArgs(_mouse, 0, _inputRoot,
                RawPointerEventType.LeaveWindow, new Point(), RawInputModifiers.None));
        }

        void IWSurfaceEventSink.OnPointerMotion(ulong timestamp, Point position, RawInputModifiers modifiers)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawPointerEventArgs(_mouse, timestamp, _inputRoot,
                RawPointerEventType.Move, position, modifiers));
        }

        void IWSurfaceEventSink.OnPointerButton(ulong timestamp, uint serial, RawPointerEventType type,
            RawInputModifiers modifiers, Point position, object? platformCookie)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawPointerEventArgs(_mouse, timestamp, _inputRoot,
                type, position, modifiers) { PlatformInputEventCookie = platformCookie });
        }

        void IWSurfaceEventSink.OnPointerAxis(ulong timestamp, Vector delta, RawInputModifiers modifiers,
            Point position)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawMouseWheelEventArgs(_mouse, timestamp, _inputRoot,
                position, delta, modifiers));
        }

        void IWSurfaceEventSink.OnTouchDown(ulong timestamp, int touchId, Point position, object? platformCookie)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawTouchEventArgs(_touch, timestamp, _inputRoot,
                    RawPointerEventType.TouchBegin, position, RawInputModifiers.None, touchId)
                { PlatformInputEventCookie = platformCookie });
        }

        void IWSurfaceEventSink.OnTouchMove(ulong timestamp, int touchId, Point position)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawTouchEventArgs(_touch, timestamp, _inputRoot,
                RawPointerEventType.TouchUpdate, position, RawInputModifiers.None, touchId));
        }

        void IWSurfaceEventSink.OnTouchUp(ulong timestamp, int touchId, Point position)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawTouchEventArgs(_touch, timestamp, _inputRoot,
                RawPointerEventType.TouchEnd, position, RawInputModifiers.None, touchId));
        }

        void IWSurfaceEventSink.OnTouchCancel(int touchId, Point position)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawTouchEventArgs(_touch, 0, _inputRoot,
                RawPointerEventType.TouchCancel, position, RawInputModifiers.None, touchId));
        }
    }
}
