using System;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Threading;
using NWayland.Interop;
using NWayland.Protocols.PointerGesturesUnstableV1;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    internal class WlPointerHandler : WlPointer.IEvents, ZwpPointerGesturePinchV1.IEvents, ZwpPointerGestureSwipeV1.IEvents, IDisposable
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly ICursorFactory _cursorFactory;
        private readonly WlInputDevice _wlInputDevice;
        private readonly WlPointer _wlPointer;
        private readonly WlSurface _pointerSurface;
        private readonly ZwpPointerGesturePinchV1? _zwpPointerGesturePinch;
        private readonly ZwpPointerGestureSwipeV1? _zwpPointerGestureSwipe;

        private Point _pointerPosition;
        private int _currentCursorImageIndex;
        private WlCursor? _currentCursor;
        private IDisposable? _pointerTimer;

        private WlWindow? _pointerWindow;
        private WlWindow? _pinchGestureWindow;
        private WlWindow? _swipeGestureWindow;

        public WlPointerHandler(AvaloniaWaylandPlatform platform, WlInputDevice wlInputDevice)
        {
            _platform = platform;
            _wlInputDevice = wlInputDevice;
            _wlPointer = platform.WlSeat.GetPointer();
            _wlPointer.Events = this;
            _cursorFactory = AvaloniaLocator.Current.GetRequiredService<ICursorFactory>();
            _pointerSurface = platform.WlCompositor.CreateSurface();
            MouseDevice = new MouseDevice();
            if (_platform.ZwpPointerGestures is null)
                return;
            _zwpPointerGesturePinch = _platform.ZwpPointerGestures.GetPinchGesture(_wlPointer);
            _zwpPointerGestureSwipe = _platform.ZwpPointerGestures.GetSwipeGesture(_wlPointer);
            _zwpPointerGesturePinch.Events = this;
            _zwpPointerGestureSwipe.Events = this;
        }

        public MouseDevice MouseDevice { get; }

        public uint PointerSurfaceSerial { get; private set; }

        public void OnEnter(WlPointer eventSender, uint serial, WlSurface surface, WlFixed surfaceX, WlFixed surfaceY)
        {
            _wlInputDevice.Serial = serial;
            PointerSurfaceSerial = serial;
            _pointerWindow = _platform.WlScreens.WindowFromSurface(surface);
            _pointerPosition = new Point((double)surfaceX, (double)surfaceY);
            if (_pointerWindow?.InputRoot is null)
                return;
            var args = new RawPointerEventArgs(MouseDevice, 0, _pointerWindow.InputRoot, RawPointerEventType.Move, _pointerPosition, _wlInputDevice.RawInputModifiers);
            _platform.WlRawEventGrouper.HandleEvent(new RawPointerEventArgsWithWindow(_pointerWindow, args));
        }

        public void OnLeave(WlPointer eventSender, uint serial, WlSurface surface)
        {
            var window = _platform.WlScreens.WindowFromSurface(surface);
            _pointerWindow = null;
            _currentCursor = null;
            PointerSurfaceSerial = serial;
            if (window?.InputRoot is null)
                return;
            var args = new RawPointerEventArgs(MouseDevice, 0, window.InputRoot, RawPointerEventType.LeaveWindow, _pointerPosition, _wlInputDevice.RawInputModifiers);
            _platform.WlRawEventGrouper.HandleEvent(new RawPointerEventArgsWithWindow(window, args));
        }

        public void OnMotion(WlPointer eventSender, uint time, WlFixed surfaceX, WlFixed surfaceY)
        {
            _pointerPosition = new Point((double)surfaceX, (double)surfaceY);
            if (_pointerWindow?.InputRoot is null)
                return;
            var args = new RawPointerEventArgs(MouseDevice, time, _pointerWindow.InputRoot, RawPointerEventType.Move, _pointerPosition, _wlInputDevice.RawInputModifiers);
            _platform.WlRawEventGrouper.HandleEvent(new RawPointerEventArgsWithWindow(_pointerWindow, args));
        }

        public void OnButton(WlPointer eventSender, uint serial, uint time, uint button, WlPointer.ButtonStateEnum state)
        {
            _wlInputDevice.Serial = serial;
            RawPointerEventType type;
            switch (button)
            {
                case (uint)EvKey.BTN_LEFT when state == WlPointer.ButtonStateEnum.Pressed:
                    type = RawPointerEventType.LeftButtonDown;
                    _wlInputDevice.RawInputModifiers |= RawInputModifiers.LeftMouseButton;
                    _wlInputDevice.UserActionDownSerial = serial;
                    break;
                case (uint)EvKey.BTN_LEFT when state == WlPointer.ButtonStateEnum.Released:
                    type = RawPointerEventType.LeftButtonUp;
                    _wlInputDevice.RawInputModifiers &= ~RawInputModifiers.LeftMouseButton;
                    break;
                case (uint)EvKey.BTN_RIGHT when state == WlPointer.ButtonStateEnum.Pressed:
                    type = RawPointerEventType.RightButtonDown;
                    _wlInputDevice.RawInputModifiers |= RawInputModifiers.RightMouseButton;
                    _wlInputDevice.UserActionDownSerial = serial;
                    break;
                case (uint)EvKey.BTN_RIGHT when state == WlPointer.ButtonStateEnum.Released:
                    type = RawPointerEventType.RightButtonUp;
                    _wlInputDevice.RawInputModifiers &= ~RawInputModifiers.RightMouseButton;
                    break;
                case (uint)EvKey.BTN_MIDDLE when state == WlPointer.ButtonStateEnum.Pressed:
                    type = RawPointerEventType.MiddleButtonDown;
                    _wlInputDevice.RawInputModifiers |= RawInputModifiers.MiddleMouseButton;
                    _wlInputDevice.UserActionDownSerial = serial;
                    break;
                case (uint)EvKey.BTN_MIDDLE when state == WlPointer.ButtonStateEnum.Released:
                    type = RawPointerEventType.MiddleButtonUp;
                    _wlInputDevice.RawInputModifiers &= ~RawInputModifiers.MiddleMouseButton;
                    break;
                case (uint)EvKey.BTN_SIDE when state == WlPointer.ButtonStateEnum.Pressed:
                    type = RawPointerEventType.XButton2Down;
                    _wlInputDevice.RawInputModifiers |= RawInputModifiers.XButton2MouseButton;
                    _wlInputDevice.UserActionDownSerial = serial;
                    break;
                case (uint)EvKey.BTN_SIDE when state == WlPointer.ButtonStateEnum.Released:
                    type = RawPointerEventType.XButton2Up;
                    _wlInputDevice.RawInputModifiers &= ~RawInputModifiers.XButton2MouseButton;
                    break;
                case (uint)EvKey.BTN_EXTRA when state == WlPointer.ButtonStateEnum.Pressed:
                    type = RawPointerEventType.XButton1Down;
                    _wlInputDevice.RawInputModifiers |= RawInputModifiers.XButton1MouseButton;
                    _wlInputDevice.UserActionDownSerial = serial;
                    break;
                case (uint)EvKey.BTN_EXTRA when state == WlPointer.ButtonStateEnum.Released:
                    type = RawPointerEventType.XButton1Up;
                    _wlInputDevice.RawInputModifiers &= ~RawInputModifiers.XButton1MouseButton;
                    break;
                default:
                    return;
            }

            if (_pointerWindow?.InputRoot is null)
                return;
            var args = new RawPointerEventArgs(MouseDevice, time, _pointerWindow.InputRoot, type, _pointerPosition, _wlInputDevice.RawInputModifiers);
            _platform.WlRawEventGrouper.HandleEvent(new RawPointerEventArgsWithWindow(_pointerWindow, args));
        }

        public void OnAxis(WlPointer eventSender, uint time, WlPointer.AxisEnum axis, WlFixed value)
        {
            if (_pointerWindow?.InputRoot is null)
                return;
            const double scrollFactor = 0.1;
            var scrollValue = -(double)value * scrollFactor;
            var delta = axis == WlPointer.AxisEnum.HorizontalScroll ? new Vector(scrollValue, 0) : new Vector(0, scrollValue);
            var args = new RawMouseWheelEventArgs(MouseDevice, time, _pointerWindow.InputRoot, _pointerPosition, delta, _wlInputDevice.RawInputModifiers);
            _platform.WlRawEventGrouper.HandleEvent(new RawPointerEventArgsWithWindow(_pointerWindow, args));
        }

        public void OnFrame(WlPointer eventSender) { }

        public void OnAxisSource(WlPointer eventSender, WlPointer.AxisSourceEnum axisSource) { }

        public void OnAxisStop(WlPointer eventSender, uint time, WlPointer.AxisEnum axis) { }

        public void OnAxisDiscrete(WlPointer eventSender, WlPointer.AxisEnum axis, int discrete) { }

        public void OnAxisValue120(WlPointer eventSender, WlPointer.AxisEnum axis, int value120) { }

        public void OnAxisRelativeDirection(WlPointer eventSender, WlPointer.AxisEnum axis, WlPointer.AxisRelativeDirectionEnum direction) { }

        public void OnBegin(ZwpPointerGesturePinchV1 eventSender, uint serial, uint time, WlSurface surface, uint fingers)
        {
            _wlInputDevice.Serial = serial;
            _pinchGestureWindow = _platform.WlScreens.WindowFromSurface(surface);
        }

        public void OnUpdate(ZwpPointerGesturePinchV1 eventSender, uint time, WlFixed dx, WlFixed dy, WlFixed scale, WlFixed rotation)
        {
            if (_pinchGestureWindow?.InputRoot is null)
                return;

            var deltaMagnify = new Vector((double)dx, (double)dy) * (double)scale;
            if (deltaMagnify != Vector.Zero)
            {
                var magnifyArgs = new RawPointerGestureEventArgs(MouseDevice, time, _pinchGestureWindow.InputRoot, RawPointerEventType.Magnify, _pointerPosition, deltaMagnify, _wlInputDevice.RawInputModifiers);
                _platform.WlRawEventGrouper.HandleEvent(new RawPointerEventArgsWithWindow(_pinchGestureWindow, magnifyArgs));
            }

            var rad = Math.PI / 180 * (double)rotation * (double)scale;
            var deltaRotation = new Vector(Math.Cos(rad), Math.Sin(rad));
            if (deltaRotation != Vector.Zero)
            {
                var rotateArgs = new RawPointerGestureEventArgs(MouseDevice, time, _pinchGestureWindow.InputRoot, RawPointerEventType.Rotate, _pointerPosition, deltaRotation, _wlInputDevice.RawInputModifiers);
                _platform.WlRawEventGrouper.HandleEvent(new RawPointerEventArgsWithWindow(_pinchGestureWindow, rotateArgs));
            }
        }

        public void OnEnd(ZwpPointerGesturePinchV1 eventSender, uint serial, uint time, int cancelled)
        {
            _wlInputDevice.Serial = serial;
        }

        public void OnBegin(ZwpPointerGestureSwipeV1 eventSender, uint serial, uint time, WlSurface surface, uint fingers)
        {
            _wlInputDevice.Serial = serial;
            _swipeGestureWindow = _platform.WlScreens.WindowFromSurface(surface);
        }

        public void OnUpdate(ZwpPointerGestureSwipeV1 eventSender, uint time, WlFixed dx, WlFixed dy)
        {
            if (_swipeGestureWindow?.InputRoot is null)
                return;
            var deltaSwipe = new Vector((double)dx, (double)dy);
            var args = new RawPointerGestureEventArgs(MouseDevice, time, _swipeGestureWindow.InputRoot, RawPointerEventType.Swipe, _pointerPosition, deltaSwipe, _wlInputDevice.RawInputModifiers);
            _platform.WlRawEventGrouper.HandleEvent(new RawPointerEventArgsWithWindow(_swipeGestureWindow, args));
        }

        public void OnEnd(ZwpPointerGestureSwipeV1 eventSender, uint serial, uint time, int cancelled)
        {
            _wlInputDevice.Serial = serial;
            _swipeGestureWindow = null;
        }

        public void SetCursor(WlCursor? wlCursor)
        {
            wlCursor ??= _cursorFactory.GetCursor(StandardCursorType.Arrow) as WlCursor;
            if (wlCursor is null || wlCursor.ImageCount <= 0  || _currentCursor == wlCursor)
                return;
            _pointerTimer?.Dispose();
            _currentCursor = wlCursor;
            _currentCursorImageIndex = -1;
            if (wlCursor.ImageCount == 1)
                SetCursorImage(wlCursor[0]);
            else
                _pointerTimer = DispatcherTimer.Run(OnCursorAnimation, wlCursor[0].Delay);
        }

        public void Dispose()
        {
            _wlPointer.Dispose();
            _pointerSurface.Dispose();
            _currentCursor?.Dispose();
            _pointerTimer?.Dispose();
            _zwpPointerGestureSwipe?.Dispose();
            _zwpPointerGesturePinch?.Dispose();
            MouseDevice.Dispose();
        }

        internal void InvalidateFocus(WlWindow window)
        {
            if (_pointerWindow == window)
                _pointerWindow = null;
            if (_pinchGestureWindow == window)
                _pinchGestureWindow = null;
            if (_swipeGestureWindow == window)
                _swipeGestureWindow = null;
        }

        private bool OnCursorAnimation()
        {
            var oldImage = _currentCursorImageIndex == -1 ? null : _currentCursor![_currentCursorImageIndex];
            if (++_currentCursorImageIndex >= _currentCursor!.ImageCount)
                _currentCursorImageIndex = 0;
            var newImage = _currentCursor[_currentCursorImageIndex];
            SetCursorImage(newImage);
            if (oldImage is null || oldImage.Delay == newImage.Delay)
                return true;
            _pointerTimer?.Dispose();
            _pointerTimer = DispatcherTimer.Run(OnCursorAnimation, newImage.Delay);
            return false;
        }

        private void SetCursorImage(WlCursor.WlCursorImage cursorImage)
        {
            _pointerSurface.Attach(cursorImage.WlBuffer, 0, 0);
            _pointerSurface.DamageBuffer(0, 0, cursorImage.Size.Width, cursorImage.Size.Height);
            _pointerSurface.Commit();
            _wlPointer.SetCursor(PointerSurfaceSerial, _pointerSurface, cursorImage.Hotspot.X, cursorImage.Hotspot.Y);
        }
    }
}
