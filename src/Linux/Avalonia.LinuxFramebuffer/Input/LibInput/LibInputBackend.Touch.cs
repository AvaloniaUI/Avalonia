using System;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.LinuxFramebuffer.Input.LibInput;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods;
public partial class LibInputBackend
{
    private readonly TouchDevice _touch = new();

    private void HandleTouch(IntPtr ev, LibInputEventType type)
    {
        var tev = libinput_event_get_touch_event(ev);
        if (tev == IntPtr.Zero)
            return;
        _layoutManager.TtyGetModifiers(out var modifiers);
        if (type < LibInputEventType.LIBINPUT_EVENT_TOUCH_FRAME)
        {
            var info = _screen.ScaledSize;
            var slot = libinput_event_touch_get_slot(tev);
            Point pt;

            if (type is LibInputEventType.LIBINPUT_EVENT_TOUCH_DOWN or LibInputEventType.LIBINPUT_EVENT_TOUCH_MOTION)
            {
                var x = libinput_event_touch_get_x_transformed(tev, (int)info.Width);
                var y = libinput_event_touch_get_y_transformed(tev, (int)info.Height);
                pt = new Point(x, y);
                _pointers[slot] = pt;
            }
            else
            {
                _pointers.TryGetValue(slot, out pt);
                _pointers.Remove(slot);
            }

            var ts = libinput_event_touch_get_time_usec(tev) / 1000;
            if (_inputRoot == null)
                return;
            ScheduleInput(new RawTouchEventArgs(_touch, ts,
                _inputRoot,
                type switch
                {
                    LibInputEventType.LIBINPUT_EVENT_TOUCH_DOWN => RawPointerEventType.TouchBegin,
                    LibInputEventType.LIBINPUT_EVENT_TOUCH_UP => RawPointerEventType.TouchEnd,
                    LibInputEventType.LIBINPUT_EVENT_TOUCH_MOTION => RawPointerEventType.TouchUpdate,
                    _ => RawPointerEventType.TouchCancel
                },
                pt, modifiers, slot));
        }
    }

}
