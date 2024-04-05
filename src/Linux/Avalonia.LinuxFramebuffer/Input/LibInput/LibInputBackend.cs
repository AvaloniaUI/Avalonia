#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Logging;
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
        private readonly RawEventGrouper _rawEventGrouper;

        /// <summary>
        ///  Default constructor
        /// </summary>
        public LibInputBackend()
        {
            _options = default;
        }

        public LibInputBackend(LibInputBackendOptions options)
        {
            _options = options;
        }

        private void DispatchInput(RawInputEventArgs args)
        {
            _onInput?.Invoke(args);
            if (!args.Handled && args is RawKeyEventArgsWithText text && !string.IsNullOrEmpty(text.Text))
                _onInput?.Invoke(new RawTextInputEventArgs((IKeyboardDevice)args.Device
                    , args.Timestamp
                    , _inputRoot
                    , text.Text));
        }

        private unsafe void InputThread(IntPtr ctx, LibInputBackendOptions options)
        {
            var fd = libinput_get_fd(ctx); 
            var pfd = new pollfd { fd = fd, events = NativeUnsafeMethods.EPOLLIN };


            foreach (var f in options.Events!)
            {
                libinput_path_add_device(ctx, f);
            }

            while (true)
            {
                if (NativeUnsafeMethods.poll(&pfd, 1, 200) < 0)
                {
                    Logger.TryGet(LogEventLevel.Error,LibInput)
                        ?.Log(this,$"Pool err{Marshal.GetLastWin32Error()}");
                }
                libinput_dispatch(ctx);
                IntPtr ev;
                while ((ev = libinput_get_event(ctx)) != IntPtr.Zero)
                {
                    var type = libinput_event_get_type(ev);
#if DEBUG
                    Logger.TryGet(LogEventLevel.Verbose, LibInput)
                        ?.Log(this,$"Event Type {type}");
#endif
                    switch (type)
                    {
                        case LibInputEventType.LIBINPUT_EVENT_DEVICE_REMOVED:
                        {
                           var  dev = libinput_event_get_device(ev);
                           libinput_device_uref(dev);
                        } break;
                        case LibInputEventType.LIBINPUT_EVENT_DEVICE_ADDED:
                        {
                            var  dev = libinput_event_get_device(ev);
                            libinput_device_ref(dev);
                        } break;
                            
                        case >= LibInputEventType.LIBINPUT_EVENT_TOUCH_DOWN and <= LibInputEventType.LIBINPUT_EVENT_TOUCH_CANCEL:
                            HandleTouch(ev, type);
                            break;
                        case >= LibInputEventType.LIBINPUT_EVENT_POINTER_MOTION and <= LibInputEventType.LIBINPUT_EVENT_POINTER_SCROLL_CONTINUOUS:
                            HandlePointer(ev, type);
                            break;
                        case LibInputEventType.LIBINPUT_EVENT_KEYBOARD_KEY:
                            HandleKeyboardEvent(ev, type);
                            break;
                    }
                    libinput_event_destroy(ev);
                }
            }
        }

        private void ScheduleInput(RawInputEventArgs args)
        {
            /*
            if (args is RawPointerEventArgs mouse)
                mouse.Position = mouse.Position / RenderScaling;
            if (args is RawDragEvent drag)
                drag.Location = drag.Location / RenderScaling;
            */
            _rawEventGrouper.HandleEvent(args);
        }
        
        /// <inheritdoc />
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

        /// <inheritdoc />
        public void SetInputRoot(IInputRoot root)
        {
            _inputRoot = root;
        }
    }
}
