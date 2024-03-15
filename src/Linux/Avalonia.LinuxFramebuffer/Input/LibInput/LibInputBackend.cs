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
        private const string LibInput = nameof(Avalonia.LinuxFramebuffer) + "/" + nameof(Avalonia.LinuxFramebuffer.Input) + "/" + nameof(LibInput);
        private Action<RawInputEventArgs> _onInput;

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
