using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods;

namespace Avalonia.LinuxFramebuffer.Input.LibInput;

public partial class LibInputBackend
{
    private readonly Dictionary<int, Point> _pointers = new();
    private readonly TouchDevice _touch = new();

    private void HandleTouch(IntPtr ev, LibInputEventType type)
    {
        var tev = libinput_event_get_touch_event(ev);
        if (tev == IntPtr.Zero)
            return;
        if (type < LibInputEventType.LIBINPUT_EVENT_TOUCH_FRAME)
        {
            var info = _screen.ScaledSize;
            var slot = libinput_event_touch_get_slot(tev);
            Point pt;

            if (type == LibInputEventType.LIBINPUT_EVENT_TOUCH_DOWN
                || type == LibInputEventType.LIBINPUT_EVENT_TOUCH_MOTION)
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
                type == LibInputEventType.LIBINPUT_EVENT_TOUCH_DOWN ? RawPointerEventType.TouchBegin
                : type == LibInputEventType.LIBINPUT_EVENT_TOUCH_UP ? RawPointerEventType.TouchEnd
                : type == LibInputEventType.LIBINPUT_EVENT_TOUCH_MOTION ? RawPointerEventType.TouchUpdate
                : RawPointerEventType.TouchCancel,
                pt, RawInputModifiers.None, slot));
        }
    }
}
