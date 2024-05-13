using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using NWayland.Interop;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    internal class WlTouchHandler : WlTouch.IEvents, IDisposable
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly WlInputDevice _wlInputDevice;
        private readonly WlTouch _wlTouch;
        private readonly Dictionary<WlTouch, int> _touchIds;

        private Point _touchPosition;
        private WlWindow? _window;

        public WlTouchHandler(AvaloniaWaylandPlatform platform, WlInputDevice wlInputDevice)
        {
            _platform = platform;
            _wlInputDevice = wlInputDevice;
            _wlTouch = platform.WlSeat.GetTouch();
            _wlTouch.Events = this;
            _touchIds = new Dictionary<WlTouch, int>();
            TouchDevice = new TouchDevice();
        }

        public TouchDevice TouchDevice { get; }

        public void OnDown(WlTouch eventSender, uint serial, uint time, WlSurface surface, int id, WlFixed x, WlFixed y)
        {
            _wlInputDevice.Serial = serial;
            _wlInputDevice.UserActionDownSerial = serial;
            _touchIds.Add(eventSender, id);
            _window = _platform.WlScreens.WindowFromSurface(surface);
            if (_window?.InputRoot is null)
                return;
            _touchPosition = new Point((double)x, (double)y) / _window.RenderScaling;
            var args = new RawTouchEventArgs(TouchDevice, time, _window.InputRoot, RawPointerEventType.TouchBegin, _touchPosition, _wlInputDevice.RawInputModifiers, id);
            _window.Input?.Invoke(args);
        }

        public void OnUp(WlTouch eventSender, uint serial, uint time, int id)
        {
            _wlInputDevice.Serial = serial;
            _touchIds.Remove(eventSender);
            if (_window?.InputRoot is null)
                return;
            var args = new RawTouchEventArgs(TouchDevice, time, _window.InputRoot, RawPointerEventType.TouchEnd, _touchPosition, _wlInputDevice.RawInputModifiers, id);
            _window.Input?.Invoke(args);
        }

        public void OnMotion(WlTouch eventSender, uint time, int id, WlFixed x, WlFixed y)
        {
            if (_window?.InputRoot is null)
                return;
            _touchPosition = new Point((double)x, (double)y) / _window.RenderScaling;
            var args = new RawTouchEventArgs(TouchDevice, time, _window.InputRoot, RawPointerEventType.TouchUpdate, _touchPosition, _wlInputDevice.RawInputModifiers, id);
            _window.Input?.Invoke(args);
        }

        public void OnFrame(WlTouch eventSender) { }

        public void OnCancel(WlTouch eventSender)
        {
            if (_window?.InputRoot is null || !_touchIds.TryGetValue(eventSender, out var id))
                return;
            var args = new RawTouchEventArgs(TouchDevice, 0, _window.InputRoot, RawPointerEventType.TouchCancel, _touchPosition, _wlInputDevice.RawInputModifiers, id);
            _window.Input?.Invoke(args);
        }

        public void OnShape(WlTouch eventSender, int id, WlFixed major, WlFixed minor) { }

        public void OnOrientation(WlTouch eventSender, int id, WlFixed orientation) { }

        public void Dispose()
        {
            _wlTouch.Dispose();
            TouchDevice.Dispose();
        }

        internal void InvalidateFocus(WlWindow window)
        {
            if (_window == window)
                _window = null;
        }
    }
}
