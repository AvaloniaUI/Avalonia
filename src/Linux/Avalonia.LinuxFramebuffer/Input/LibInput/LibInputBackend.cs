using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Avalonia.Input;
using Avalonia.Input.Raw;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods;
namespace Avalonia.LinuxFramebuffer.Input.LibInput
{
    public partial class LibInputBackend : IInputBackend
    {
        private IScreenInfoProvider _screen;
        private IInputRoot _inputRoot;
        private TouchDevice _touch = new TouchDevice();
        private const string LibInput = nameof(Avalonia.LinuxFramebuffer) + "/" + nameof(Avalonia.LinuxFramebuffer.Input) + "/" + nameof(LibInput);
        private Action<RawInputEventArgs> _onInput;
        private Dictionary<int, Point> _pointers = new Dictionary<int, Point>();

        public LibInputBackend()
        {
            var ctx = libinput_path_create_context();
            new Thread(() => InputThread(ctx))
            {
                IsBackground = true
            }.Start();
        }

        private unsafe void InputThread(IntPtr ctx)
        {
            var fd = libinput_get_fd(ctx);

            foreach (var f in Directory.GetFiles("/dev/input", "event*"))
                libinput_path_add_device(ctx, f);
            while (true)
            {
                IntPtr ev;
                libinput_dispatch(ctx);
                while ((ev = libinput_get_event(ctx)) != IntPtr.Zero)
                {

                    var type = libinput_event_get_type(ev);
                    if (type >= LibInputEventType.LIBINPUT_EVENT_TOUCH_DOWN &&
                        type <= LibInputEventType.LIBINPUT_EVENT_TOUCH_CANCEL)
                        HandleTouch(ev, type);

                    if (type >= LibInputEventType.LIBINPUT_EVENT_POINTER_MOTION
                        && type <= LibInputEventType.LIBINPUT_EVENT_POINTER_AXIS)
                        HandlePointer(ev, type);

                    libinput_event_destroy(ev);
                    libinput_dispatch(ctx);
                }

                pollfd pfd = new pollfd { fd = fd, events = 1 };
                NativeUnsafeMethods.poll(&pfd, new IntPtr(1), 10);
            }
        }

        private void ScheduleInput(RawInputEventArgs ev) => _onInput.Invoke(ev);

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

        public void Initialize(IScreenInfoProvider screen, Action<RawInputEventArgs> onInput)
        {
            _screen = screen;
            _onInput = onInput;
        }

        public void SetInputRoot(IInputRoot root)
        {
            _inputRoot = root;
        }
    }
}
