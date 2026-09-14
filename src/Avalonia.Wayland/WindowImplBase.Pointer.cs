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
            if (InputRoot is null)
                return;
            ScheduleInput(new RawPointerEventArgs(Mouse, timestamp, InputRoot,
                RawPointerEventType.Move, position, RawInputModifiers.None));
        }

        void IWSurfaceEventSink.OnPointerLeave(uint serial)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(new RawPointerEventArgs(Mouse, 0, InputRoot,
                RawPointerEventType.LeaveWindow, new Point(), RawInputModifiers.None));
        }

        void IWSurfaceEventSink.OnPointerMotion(ulong timestamp, Point position, RawInputModifiers modifiers)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(new RawPointerEventArgs(Mouse, timestamp, InputRoot,
                RawPointerEventType.Move, position, modifiers));
        }

        void IWSurfaceEventSink.OnPointerButton(ulong timestamp, uint serial, RawPointerEventType type,
            RawInputModifiers modifiers, Point position, object? platformCookie)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(new RawPointerEventArgs(Mouse, timestamp, InputRoot,
                type, position, modifiers) { PlatformInputEventCookie = platformCookie });
        }

        void IWSurfaceEventSink.OnPointerAxis(ulong timestamp, Vector delta, RawInputModifiers modifiers,
            Point position)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(new RawMouseWheelEventArgs(Mouse, timestamp, InputRoot,
                position, delta, modifiers));
        }

        void IWSurfaceEventSink.OnTouchDown(ulong timestamp, int touchId, Point position, object? platformCookie)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(new RawTouchEventArgs(Touch, timestamp, InputRoot,
                    RawPointerEventType.TouchBegin, position, RawInputModifiers.None, touchId)
                { PlatformInputEventCookie = platformCookie });
        }

        void IWSurfaceEventSink.OnTouchMove(ulong timestamp, int touchId, Point position)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(new RawTouchEventArgs(Touch, timestamp, InputRoot,
                RawPointerEventType.TouchUpdate, position, RawInputModifiers.None, touchId));
        }

        void IWSurfaceEventSink.OnTouchUp(ulong timestamp, int touchId, Point position)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(new RawTouchEventArgs(Touch, timestamp, InputRoot,
                RawPointerEventType.TouchEnd, position, RawInputModifiers.None, touchId));
        }

        void IWSurfaceEventSink.OnTouchCancel(int touchId, Point position)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(new RawTouchEventArgs(Touch, 0, InputRoot,
                RawPointerEventType.TouchCancel, position, RawInputModifiers.None, touchId));
        }
    }
}
