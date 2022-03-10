using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods; 
namespace Avalonia.LinuxFramebuffer.Input.LibInput
{
    public class LibInputBackend : IInputBackend
    {
        private IScreenInfoProvider _screen;
        private IInputRoot _inputRoot;
        private readonly Queue<Action> _inputThreadActions = new Queue<Action>();
        private TouchDevice _touch = new TouchDevice();
        private MouseDevice _mouse = new MouseDevice();
        private Point _mousePosition;

        private readonly RawEventGroupingThreadingHelper _inputQueue;
        private Action<RawInputEventArgs> _onInput;
        private Dictionary<int, Point> _pointers = new Dictionary<int, Point>();

        public LibInputBackend()
        {
            var ctx = libinput_path_create_context();
            _inputQueue = new(e => _onInput?.Invoke(e));
            new Thread(()=>InputThread(ctx)).Start();
        }

        
        
        private unsafe void InputThread(IntPtr ctx)
        {
            var fd = libinput_get_fd(ctx);
            
            var timeval = stackalloc IntPtr[2];


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

                pollfd pfd = new pollfd {fd = fd, events = 1};
                NativeUnsafeMethods.poll(&pfd, new IntPtr(1), 10);
            }
        }

        private void ScheduleInput(RawInputEventArgs ev) => _inputQueue.OnEvent(ev);

        private void HandleTouch(IntPtr ev, LibInputEventType type)
        {
            var tev = libinput_event_get_touch_event(ev);
            if(tev == IntPtr.Zero)
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

        private void HandlePointer(IntPtr ev, LibInputEventType type)
        {
            //TODO: support input modifiers
            var pev = libinput_event_get_pointer_event(ev);
            var info = _screen.ScaledSize;
            var ts = libinput_event_pointer_get_time_usec(pev) / 1000;
            if (type == LibInputEventType.LIBINPUT_EVENT_POINTER_MOTION_ABSOLUTE)
            {
                _mousePosition = new Point(libinput_event_pointer_get_absolute_x_transformed(pev, (int)info.Width),
                    libinput_event_pointer_get_absolute_y_transformed(pev, (int)info.Height));
                ScheduleInput(new RawPointerEventArgs(_mouse, ts, _inputRoot, RawPointerEventType.Move, _mousePosition,
                    RawInputModifiers.None));
            }
            else if (type == LibInputEventType.LIBINPUT_EVENT_POINTER_BUTTON)
            {
                var button = (EvKey)libinput_event_pointer_get_button(pev);
                var buttonState = libinput_event_pointer_get_button_state(pev);


                var evnt = button == EvKey.BTN_LEFT ?
                    (buttonState == 1 ? RawPointerEventType.LeftButtonDown : RawPointerEventType.LeftButtonUp) :
                    button == EvKey.BTN_MIDDLE ?
                        (buttonState == 1 ? RawPointerEventType.MiddleButtonDown : RawPointerEventType.MiddleButtonUp) :
                        button == EvKey.BTN_RIGHT ?
                            (buttonState == 1 ?
                                RawPointerEventType.RightButtonDown :
                                RawPointerEventType.RightButtonUp) :
                            (RawPointerEventType)(-1);
                if (evnt == (RawPointerEventType)(-1))
                    return;
                        

                ScheduleInput(
                    new RawPointerEventArgs(_mouse, ts, _inputRoot, evnt, _mousePosition, RawInputModifiers.None));
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
