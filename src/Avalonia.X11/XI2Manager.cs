using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Input;
using Avalonia.Input.Raw;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    unsafe class XI2Manager
    {
        private X11Info _x11;
        private bool _multitouch;
        private Dictionary<IntPtr, IXI2Client> _clients = new Dictionary<IntPtr, IXI2Client>();
        class DeviceInfo
        {
            public int Id { get; }
            public XIValuatorClassInfo[] Valuators { get; private set; }
            public XIScrollClassInfo[] Scrollers { get; private set; } 
            public DeviceInfo(XIDeviceInfo info)
            {
                Id = info.Deviceid;
                Update(info.Classes, info.NumClasses);
            }

            public virtual void Update(XIAnyClassInfo** classes, int num)
            {
                var valuators = new List<XIValuatorClassInfo>();
                var scrollers = new List<XIScrollClassInfo>();
                for (var c = 0; c < num; c++)
                {
                    if (classes[c]->Type == XiDeviceClass.XIValuatorClass)
                        valuators.Add(*((XIValuatorClassInfo**)classes)[c]);
                    if (classes[c]->Type == XiDeviceClass.XIScrollClass)
                        scrollers.Add(*((XIScrollClassInfo**)classes)[c]);
                }

                Valuators = valuators.ToArray();
                Scrollers = scrollers.ToArray();
            }

            public void UpdateValuators(Dictionary<int, double> valuators)
            {
                foreach (var v in valuators)
                {
                    if (Valuators.Length > v.Key) 
                        Valuators[v.Key].Value = v.Value;
                }
            }
        }

        class PointerDeviceInfo : DeviceInfo
        {
            public PointerDeviceInfo(XIDeviceInfo info) : base(info)
            {
            }

            public bool HasScroll(ParsedDeviceEvent ev)
            {
                foreach (var val in ev.Valuators)
                    if (Scrollers.Any(s => s.Number == val.Key))
                        return true;

                return false;
            }
            
            public bool HasMotion(ParsedDeviceEvent ev)
            {
                foreach (var val in ev.Valuators)
                    if (Scrollers.All(s => s.Number != val.Key))
                        return true;

                return false;
            }
            
        }
        
        private PointerDeviceInfo _pointerDevice;
        private AvaloniaX11Platform _platform;
        private readonly TouchDevice _touchDevice = new TouchDevice();


        public bool Init(AvaloniaX11Platform platform)
        {
            _platform = platform;
            _x11 = platform.Info;
            _multitouch = platform.Options?.EnableMultiTouch ?? false;
            var devices =(XIDeviceInfo*) XIQueryDevice(_x11.Display,
                (int)XiPredefinedDeviceId.XIAllMasterDevices, out int num);
            for (var c = 0; c < num; c++)
            {
                if (devices[c].Use == XiDeviceType.XIMasterPointer)
                {
                    _pointerDevice = new PointerDeviceInfo(devices[c]);
                    break;
                }
            }
            if(_pointerDevice == null)
                return false;
            /*
            int mask = 0;
            
            XISetMask(ref mask, XiEventType.XI_DeviceChanged);
            var emask = new XIEventMask
            {
                Mask = &mask,
                Deviceid = _pointerDevice.Id,
                MaskLen = XiEventMaskLen
            };
            
            if (XISelectEvents(_x11.Display, _x11.RootWindow, &emask, 1) != Status.Success)
                return false;
            return true;
            */
            return XiSelectEvents(_x11.Display, _x11.RootWindow, new Dictionary<int, List<XiEventType>>
            {
                [_pointerDevice.Id] = new List<XiEventType>
                {
                    XiEventType.XI_DeviceChanged
                }
            }) == Status.Success;
        }

        public XEventMask AddWindow(IntPtr xid, IXI2Client window)
        {
            _clients[xid] = window;
            var events = new List<XiEventType>
            {
                XiEventType.XI_Motion,
                XiEventType.XI_ButtonPress,
                XiEventType.XI_ButtonRelease
            };

            if (_multitouch)
                events.AddRange(new[]
                {
                    XiEventType.XI_TouchBegin,
                    XiEventType.XI_TouchUpdate,
                    XiEventType.XI_TouchEnd
                });

            XiSelectEvents(_x11.Display, xid,
                new Dictionary<int, List<XiEventType>> {[_pointerDevice.Id] = events});
                
            // We are taking over mouse input handling from here
            return XEventMask.PointerMotionMask
                   | XEventMask.ButtonMotionMask
                   | XEventMask.Button1MotionMask
                   | XEventMask.Button2MotionMask
                   | XEventMask.Button3MotionMask
                   | XEventMask.Button4MotionMask
                   | XEventMask.Button5MotionMask
                   | XEventMask.ButtonPressMask
                   | XEventMask.ButtonReleaseMask;
        }

        public void OnWindowDestroyed(IntPtr xid) => _clients.Remove(xid);
        
        public void OnEvent(XIEvent* xev)
        {
            if (xev->evtype == XiEventType.XI_DeviceChanged)
            {
                var changed = (XIDeviceChangedEvent*)xev;
                _pointerDevice.Update(changed->Classes, changed->NumClasses);
            }

            
            if ((xev->evtype >= XiEventType.XI_ButtonPress && xev->evtype <= XiEventType.XI_Motion)
                || (xev->evtype>=XiEventType.XI_TouchBegin&&xev->evtype<=XiEventType.XI_TouchEnd))
            {
                var dev = (XIDeviceEvent*)xev;
                if (_clients.TryGetValue(dev->EventWindow, out var client))
                    OnDeviceEvent(client, new ParsedDeviceEvent(dev));
            }
        }

        void OnDeviceEvent(IXI2Client client, ParsedDeviceEvent ev)
        {
            if (ev.Type == XiEventType.XI_TouchBegin 
                || ev.Type == XiEventType.XI_TouchUpdate 
                || ev.Type == XiEventType.XI_TouchEnd)
            {
                var type = ev.Type == XiEventType.XI_TouchBegin ?
                    RawPointerEventType.TouchBegin :
                    (ev.Type == XiEventType.XI_TouchUpdate ?
                        RawPointerEventType.TouchUpdate :
                        RawPointerEventType.TouchEnd);
                client.ScheduleInput(new RawTouchEventArgs(_touchDevice,
                    ev.Timestamp, client.InputRoot, type, ev.Position, ev.Modifiers, ev.Detail));
                return;
            }

            if (_multitouch && ev.Emulated)
                return;
            
            if (ev.Type == XiEventType.XI_Motion)
            {
                Vector scrollDelta = default;
                foreach (var v in ev.Valuators)
                {
                    foreach (var scroller in _pointerDevice.Scrollers)
                    {
                        if (scroller.Number == v.Key)
                        {
                            var old = _pointerDevice.Valuators[scroller.Number].Value;
                            // Value was zero after reset, ignore the event and use it as a reference next time
                            if (old == 0)
                                continue;
                            var diff = (old - v.Value) / scroller.Increment;
                            if (scroller.ScrollType == XiScrollType.Horizontal)
                                scrollDelta = scrollDelta.WithX(scrollDelta.X + diff);
                            else
                                scrollDelta = scrollDelta.WithY(scrollDelta.Y + diff);

                        }
                    }


                }

                if (scrollDelta != default)
                    client.ScheduleInput(new RawMouseWheelEventArgs(_platform.MouseDevice, ev.Timestamp,
                        client.InputRoot, ev.Position, scrollDelta, ev.Modifiers));
                if (_pointerDevice.HasMotion(ev))
                    client.ScheduleInput(new RawPointerEventArgs(_platform.MouseDevice, ev.Timestamp, client.InputRoot,
                        RawPointerEventType.Move, ev.Position, ev.Modifiers));
            }

            if (ev.Type == XiEventType.XI_ButtonPress || ev.Type == XiEventType.XI_ButtonRelease)
            {
                var down = ev.Type == XiEventType.XI_ButtonPress;
                var type =
                    ev.Button == 1 ? (down ? RawPointerEventType.LeftButtonDown : RawPointerEventType.LeftButtonUp)
                    : ev.Button == 2 ? (down ? RawPointerEventType.MiddleButtonDown : RawPointerEventType.MiddleButtonUp)
                    : ev.Button == 3 ? (down ? RawPointerEventType.RightButtonDown : RawPointerEventType.RightButtonUp)
                    : (RawPointerEventType?)null;
                if (type.HasValue)
                    client.ScheduleInput(new RawPointerEventArgs(_platform.MouseDevice, ev.Timestamp, client.InputRoot,
                        type.Value, ev.Position, ev.Modifiers));
            }
            
            _pointerDevice.UpdateValuators(ev.Valuators);
        }
    }

    unsafe class ParsedDeviceEvent
    {
        public XiEventType Type { get; }
        public InputModifiers Modifiers { get; }
        public ulong Timestamp { get; }
        public Point Position { get; }
        public int Button { get; set; }
        public int Detail { get; set; }
        public bool Emulated { get; set; }
        public Dictionary<int, double> Valuators { get; }
        public ParsedDeviceEvent(XIDeviceEvent* ev)
        {
            Type = ev->evtype;
            Timestamp = (ulong)ev->time.ToInt64();
            var state = (XModifierMask)ev->mods.Effective;
            if (state.HasFlag(XModifierMask.ShiftMask))
                Modifiers |= InputModifiers.Shift;
            if (state.HasFlag(XModifierMask.ControlMask))
                Modifiers |= InputModifiers.Control;
            if (state.HasFlag(XModifierMask.Mod1Mask))
                Modifiers |= InputModifiers.Alt;
            if (state.HasFlag(XModifierMask.Mod4Mask))
                Modifiers |= InputModifiers.Windows;

            if (ev->buttons.MaskLen > 0)
            {
                var buttons = ev->buttons.Mask;
                if (XIMaskIsSet(buttons, 1))
                    Modifiers |= InputModifiers.LeftMouseButton;
                
                if (XIMaskIsSet(buttons, 2))
                    Modifiers |= InputModifiers.MiddleMouseButton;
                
                if (XIMaskIsSet(buttons, 3))
                    Modifiers |= InputModifiers.RightMouseButton;
            }

            Valuators = new Dictionary<int, double>();
            Position = new Point(ev->event_x, ev->event_y);
            var values = ev->valuators.Values;
            for (var c = 0; c < ev->valuators.MaskLen * 8; c++)
                if (XIMaskIsSet(ev->valuators.Mask, c))
                    Valuators[c] = *values++;
            if (Type == XiEventType.XI_ButtonPress || Type == XiEventType.XI_ButtonRelease)
                Button = ev->detail;
            Detail = ev->detail;
            Emulated = ev->flags.HasFlag(XiDeviceEventFlags.XIPointerEmulated);
        }
    }
    
    interface IXI2Client
    {
        IInputRoot InputRoot { get; }
        void ScheduleInput(RawInputEventArgs args);
    }
}
