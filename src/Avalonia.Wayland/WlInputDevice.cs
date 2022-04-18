using System;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    public class WlInputDevice : WlSeat.IEvents, WlPointer.IEvents, WlKeyboard.IEvents, WlTouch.IEvents
    {
        private readonly AvaloniaWaylandPlatform _platform;

        private WlPointer? _wlPointer;
        private WlKeyboard? _wlKeyboard;
        private WlTouch? _wlTouch;
        private Point _pointerPosition;
        private IntPtr _xkbKeymap;
        private IntPtr _xkbState;

        public WlInputDevice(AvaloniaWaylandPlatform platform)
        {
            _platform = platform;
            _platform.WlSeat.Events = this;
        }

        public MouseDevice? MouseDevice { get; private set; }

        public KeyboardDevice? KeyboardDevice { get; private set; }

        public TouchDevice? TouchDevice { get; private set; }

        public IInputRoot InputRoot { get; set; }

        public Action<RawInputEventArgs>? Input { get; set; }

        public uint LastSerial { get; private set; }

        public void SetCursor(WlCursorFactory.WlCursor wlCursor)
        {
            if (_wlPointer is null) return;
            //_wlPointer.SetCursor(LastSerial, );
        }

        void WlSeat.IEvents.OnCapabilities(WlSeat eventSender, WlSeat.CapabilityEnum capabilities)
        {
            if (capabilities.HasAllFlags(WlSeat.CapabilityEnum.Pointer))
            {
                _wlPointer = _platform.WlSeat.GetPointer();
                _wlPointer.Events = this;
                MouseDevice = new MouseDevice();
            }
            if (capabilities.HasAllFlags(WlSeat.CapabilityEnum.Keyboard))
            {
                _wlKeyboard = _platform.WlSeat.GetKeyboard();
                _wlKeyboard.Events = this;
                KeyboardDevice = new KeyboardDevice();
            }
            if (capabilities.HasAllFlags(WlSeat.CapabilityEnum.Touch))
            {
                _wlTouch = _platform.WlSeat.GetTouch();
                _wlTouch.Events = this;
                TouchDevice = new TouchDevice();
            }
        }

        void WlSeat.IEvents.OnName(WlSeat eventSender, string name) { }

        void WlPointer.IEvents.OnEnter(WlPointer eventSender, uint serial, WlSurface surface, int surfaceX, int surfaceY)
        {
            _pointerPosition = new Point(surfaceX / 256, surfaceY / 256);
            LastSerial = serial;
        }

        void WlPointer.IEvents.OnLeave(WlPointer eventSender, uint serial, WlSurface surface)
        {
            LastSerial = serial;
            Input?.Invoke(new RawPointerEventArgs(MouseDevice!, 0, InputRoot, RawPointerEventType.LeaveWindow, _pointerPosition, RawInputModifiers.None));
        }

        void WlPointer.IEvents.OnMotion(WlPointer eventSender, uint time, int surfaceX, int surfaceY)
        {
            _pointerPosition = new Point(surfaceX / 256, surfaceY / 256);
            Input?.Invoke(new RawPointerEventArgs(MouseDevice!, time, InputRoot, RawPointerEventType.Move, _pointerPosition, RawInputModifiers.None));
        }

        void WlPointer.IEvents.OnButton(WlPointer eventSender, uint serial, uint time, uint button, WlPointer.ButtonStateEnum state)
        {
            LastSerial = serial;
            Input?.Invoke(new RawPointerEventArgs(MouseDevice!, time, InputRoot, ProcessButton(button, state), _pointerPosition, RawInputModifiers.None));
        }

        void WlPointer.IEvents.OnAxis(WlPointer eventSender, uint time, WlPointer.AxisEnum axis, int value)
            => Input?.Invoke(new RawMouseWheelEventArgs(MouseDevice!, time, InputRoot, _pointerPosition, GetVectorForAxis(axis, value), RawInputModifiers.None));

        void WlPointer.IEvents.OnFrame(WlPointer eventSender) { }

        void WlPointer.IEvents.OnAxisSource(WlPointer eventSender, WlPointer.AxisSourceEnum axisSource) { }

        void WlPointer.IEvents.OnAxisStop(WlPointer eventSender, uint time, WlPointer.AxisEnum axis) { }

        void WlPointer.IEvents.OnAxisDiscrete(WlPointer eventSender, WlPointer.AxisEnum axis, int discrete) { }

        void WlKeyboard.IEvents.OnKeymap(WlKeyboard eventSender, WlKeyboard.KeymapFormatEnum format, int fd, uint size)
        {
            var map = NativeMethods.mmap(IntPtr.Zero, new IntPtr(size), 0x1, 0x02, fd, IntPtr.Zero);

            if (map == new IntPtr(-1))
            {
                NativeMethods.close(fd);
                return;
            }

            var context = LibXkbCommon.xkb_context_new(0);
            var keymap = LibXkbCommon.xkb_keymap_new_from_string(context, map, (uint)format, 0);
            NativeMethods.munmap(map, new IntPtr(fd));
            NativeMethods.close(fd);

            if (keymap == IntPtr.Zero)
            {
                LibXkbCommon.xkb_context_unref(context);
                return;
            }

            LibXkbCommon.xkb_keymap_unref(_xkbKeymap);
            _xkbKeymap = keymap;
            LibXkbCommon.xkb_state_unref(_xkbState);
            _xkbState = LibXkbCommon.xkb_state_new(keymap);
            LibXkbCommon.xkb_context_unref(context);
        }

        void WlKeyboard.IEvents.OnEnter(WlKeyboard eventSender, uint serial, WlSurface surface, ReadOnlySpan<int> keys)
        {
            LastSerial = serial;
        }

        void WlKeyboard.IEvents.OnLeave(WlKeyboard eventSender, uint serial, WlSurface surface)
        {
            LastSerial = serial;
        }

        unsafe void WlKeyboard.IEvents.OnKey(WlKeyboard eventSender, uint serial, uint time, uint key, WlKeyboard.KeyStateEnum state)
        {
            LastSerial = serial;
            var code = key + 8;
            uint* syms;
            var numSyms = LibXkbCommon.xkb_state_key_get_syms(_xkbState, code, &syms);
            var sym = numSyms == 1 ? syms[0] : 0;
            var avaloniaKey = XkbKeyTransform.ConvertKey((XkbKey)sym);
            if (state == WlKeyboard.KeyStateEnum.Pressed)
            {
                Input?.Invoke(new RawKeyEventArgs(KeyboardDevice!, time, InputRoot, (RawKeyEventType)state, avaloniaKey, RawInputModifiers.None));
            }
        }

        void WlKeyboard.IEvents.OnModifiers(WlKeyboard eventSender, uint serial, uint modsDepressed, uint modsLatched, uint modsLocked, uint group)
        {
            LastSerial = serial;
        }

        void WlKeyboard.IEvents.OnRepeatInfo(WlKeyboard eventSender, int rate, int delay)
        {

        }

        void WlTouch.IEvents.OnDown(WlTouch eventSender, uint serial, uint time, WlSurface surface, int id, int x, int y)
        {
            LastSerial = serial;
        }

        void WlTouch.IEvents.OnUp(WlTouch eventSender, uint serial, uint time, int id)
        {
            LastSerial = serial;
        }

        void WlTouch.IEvents.OnMotion(WlTouch eventSender, uint time, int id, int x, int y)
        {

        }

        void WlTouch.IEvents.OnFrame(WlTouch eventSender)
        {

        }

        void WlTouch.IEvents.OnCancel(WlTouch eventSender)
        {

        }

        void WlTouch.IEvents.OnShape(WlTouch eventSender, int id, int major, int minor)
        {

        }

        void WlTouch.IEvents.OnOrientation(WlTouch eventSender, int id, int orientation)
        {

        }

        private static RawPointerEventType ProcessButton(uint button, WlPointer.ButtonStateEnum buttonState)
            => button switch
                {
                    (uint)EvKey.BTN_LEFT => buttonState == WlPointer.ButtonStateEnum.Pressed ?
                        RawPointerEventType.LeftButtonDown :
                        RawPointerEventType.LeftButtonUp,
                    (uint)EvKey.BTN_RIGHT => buttonState == WlPointer.ButtonStateEnum.Pressed ?
                        RawPointerEventType.RightButtonDown :
                        RawPointerEventType.RightButtonUp,
                    (uint)EvKey.BTN_MIDDLE => buttonState == WlPointer.ButtonStateEnum.Pressed ?
                        RawPointerEventType.MiddleButtonDown :
                        RawPointerEventType.MiddleButtonUp,
                    _ => RawPointerEventType.NonClientLeftButtonDown
                };

        private static Vector GetVectorForAxis(WlPointer.AxisEnum axis, int value)
            => axis == WlPointer.AxisEnum.HorizontalScroll ? new Vector(-value, 0) : new Vector(0, -value);
    }
}
