#nullable enable
using System;
using System.IO;
using System.Threading;
using Avalonia.Input;
using Avalonia.Input.Raw;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods;
namespace Avalonia.LinuxFramebuffer.Input.LibInput
{
    public partial class LibInputBackend : IInputBackend
    {
        private IScreenInfoProvider? _screen;
        private IInputRoot? _inputRoot;
        private const string LibInput = nameof(LinuxFramebuffer) + "/" + nameof(Input) + "/" + nameof(LibInput);
        private Action<RawInputEventArgs>? _onInput;
        private readonly LibInputBackendOptions? _options;

        public LibInputBackend()
        {
            _options = default;
        }

        public LibInputBackend(LibInputBackendOptions options)
        {
            _options = options;
        }

        private unsafe void InputThread(IntPtr ctx, LibInputBackendOptions options)
        {
            var fd = libinput_get_fd(ctx);

            foreach (var f in options.Events!)
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

                var pfd = new PollFd { fd = fd, events = 1 };
                NativeUnsafeMethods.poll(&pfd, new IntPtr(1), 10);
            }
        }

        private void ScheduleInput(RawInputEventArgs ev) => _onInput?.Invoke(ev);

        public void Initialize(IScreenInfoProvider screen, Action<RawInputEventArgs> onInput)
        {
            _screen = screen;
            _onInput = onInput;
            var ctx = libinput_path_create_context();
            var options = new LibInputBackendOptions()
            {
                Events = _options?.Events is null
                    ? Directory.GetFiles("/dev/input", "event*")
                    : _options.Events,
            };
            new Thread(() => InputThread(ctx, options))
            {
                Name = "Input Manager Worker",
                IsBackground = true
            }.Start();
        }

        public void SetInputRoot(IInputRoot root)
        {
            _inputRoot = root;
        }
    }
}
