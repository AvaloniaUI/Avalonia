using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Wayland.Clipboard;
using Avalonia.Wayland.Server.Transient.Clipboard;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Persistent;
using NWayland;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server.Transient;

partial class WaylandInputDispatcher : IDisposable
{
    private readonly WaylandGlobals _globals;
    private readonly Dictionary<uint, Seat> _seats = new();

    internal static WXdgShellSurface? FindSurfaceForWlSurface(WlSurface? surface) =>
        surface != null && surface.Tags.TryGetValue(typeof(WXdgShellSurface), out var tag)
            ? (WXdgShellSurface)tag
            : null;

    internal void NotifyCursorChanged(WSurface surface)
    {
        foreach (var seat in _seats.Values)
            seat.NotifyCursorChanged(surface);
    }

    /// <summary>
    /// Sets the pointer cursor to provide visual DnD action feedback.
    /// Called from the Wayland thread when <c>wl_data_source.action</c> fires during an active drag.
    /// </summary>
    internal void SetDndCursor(StandardCursorType cursorType)
    {
        foreach (var seat in _seats.Values)
            seat.SetDndCursor(cursorType);
    }

    /// <summary>
    /// Returns the <see cref="WlSurface"/> that currently has pointer focus.
    /// Used as the origin surface for <see cref="WlDataDevice.StartDrag"/>.
    /// </summary>
    internal WlSurface? FindOriginSurface()
    {
        foreach (var seat in _seats.Values)
        {
            if (seat.PointerFocusedWlSurface is { } surface)
                return surface;
        }
        return null;
    }

    public WaylandInputDispatcher(WaylandGlobals globals)
    {
        _globals = globals;
    }

    /// <summary>
    /// Worker-side facade for <c>zwp_text_input_v3</c>. Null until
    /// <see cref="OnInitialGlobalsBound"/> runs (and may stay null if the
    /// compositor does not advertise the manager).
    /// </summary>
    internal WaylandTextInputV3? TextInputV3 { get; private set; }

    /// <summary>
    /// Returns the first available data device, or null if no seat has one.
    /// Used by the clipboard implementation to access the active selection.
    /// </summary>
    internal WaylandDataDevice? GetDataDevice()
    {
        foreach (var seat in _seats.Values)
        {
            if (seat.DataDevice != null)
                return seat.DataDevice;
        }
        return null;
    }

    internal void OnSeatAdded(uint globalName, WlRegistry registry, uint version)
    {
        var seat = new Seat(this, registry, globalName, version);
        _seats[globalName] = seat;
        TextInputV3?.OnSeatAdded(globalName, seat.WlSeat);
    }

    /// <summary>
    /// Called once after <see cref="WaylandGlobals"/> has bound all available
    /// globals. Constructs the text-input-v3 facade if the manager is present
    /// and backfills per-seat objects (data devices, text-input-v3 proxies)
    /// for any seats that were announced before this point.
    /// </summary>
    internal void OnInitialGlobalsBound()
    {
        if (_globals.TextInputManagerV3 is { } manager)
            TextInputV3 = new WaylandTextInputV3(manager);
        foreach (var (globalName, seat) in _seats)
        {
            seat.EnsureDataDevice();
            TextInputV3?.OnSeatAdded(globalName, seat.WlSeat);
        }
    }

    internal void OnSeatRemoved(uint globalName)
    {
        TextInputV3?.OnSeatRemoved(globalName);
        if (_seats.Remove(globalName, out var seat))
            seat.Dispose();
    }
    
    public void Dispose()
    {
        foreach (var seat in _seats.Values)
            seat.Dispose();
        _seats.Clear();
        TextInputV3?.Dispose();
        TextInputV3 = null;
    }

    // Linux input-event-codes.h button constants
    private const uint BTN_LEFT = 0x110;
    private const uint BTN_RIGHT = 0x111;
    private const uint BTN_MIDDLE = 0x112;
    private const uint BTN_SIDE = 0x113;
    private const uint BTN_EXTRA = 0x114;

    private static RawPointerEventType? MapButton(uint button, bool pressed) => button switch
    {
        BTN_LEFT => pressed ? RawPointerEventType.LeftButtonDown : RawPointerEventType.LeftButtonUp,
        BTN_RIGHT => pressed ? RawPointerEventType.RightButtonDown : RawPointerEventType.RightButtonUp,
        BTN_MIDDLE => pressed ? RawPointerEventType.MiddleButtonDown : RawPointerEventType.MiddleButtonUp,
        BTN_SIDE => pressed ? RawPointerEventType.XButton1Down : RawPointerEventType.XButton1Up,
        BTN_EXTRA => pressed ? RawPointerEventType.XButton2Down : RawPointerEventType.XButton2Up,
        _ => null
    };

    private static RawInputModifiers ButtonToModifier(uint button) => button switch
    {
        BTN_LEFT => RawInputModifiers.LeftMouseButton,
        BTN_RIGHT => RawInputModifiers.RightMouseButton,
        BTN_MIDDLE => RawInputModifiers.MiddleMouseButton,
        BTN_SIDE => RawInputModifiers.XButton1MouseButton,
        BTN_EXTRA => RawInputModifiers.XButton2MouseButton,
        _ => RawInputModifiers.None
    };

    class Seat : IDisposable
    {
        private readonly WaylandInputDispatcher _dispatcher;
        private readonly WlSeat _wlSeat;
        private PointerHandler? _pointerHandler;
        private TouchHandler? _touchHandler;
        private KeyboardHandler? _keyboardHandler;

        public WlSeat WlSeat => _wlSeat;
        public WaylandDataDevice? DataDevice { get; private set; }

        /// <summary>
        /// The WlSurface currently under pointer focus, exposed for DnD origin lookup.
        /// </summary>
        internal WlSurface? PointerFocusedWlSurface => _pointerHandler?._focusedWlSurface;

        /// <summary>
        /// Current keyboard modifiers (Shift/Ctrl/Alt/Meta), updated by the keyboard handler.
        /// Read by pointer/touch handlers for combined modifier state.
        /// </summary>
        internal RawInputModifiers KeyboardModifiers;

        public Seat(WaylandInputDispatcher dispatcher, WlRegistry registry, uint globalName, uint version)
        {
            _dispatcher = dispatcher;
            _wlSeat = WlSeat.Bind(registry, globalName, version, new SeatListener(this));
            EnsureDataDevice();
        }

        /// <summary>
        /// Creates the data device for this seat if a <see cref="WlDataDeviceManager"/> is available
        /// and the device hasn't been created yet. Called from the constructor and also after
        /// the manager global becomes available (seats can be announced before the manager).
        /// </summary>
        internal void EnsureDataDevice()
        {
            if (DataDevice != null)
                return;

            var manager = _dispatcher._globals.DataDeviceManager;
            if (manager == null)
                return;

            var deviceListener = new WaylandDataDeviceListener();
            var wlDataDevice = manager.GetDataDevice(_wlSeat, deviceListener);
            DataDevice = new WaylandDataDevice(wlDataDevice, _dispatcher, _wlSeat,
                _dispatcher._globals.Connection.Display, _dispatcher._globals.Worker, () => KeyboardModifiers);
            deviceListener.SetWrapper(DataDevice);
        }

        private void OnCapabilities(WlSeat.CapabilityEnum capabilities)
        {
            var hasPointer = capabilities.HasFlag(WlSeat.CapabilityEnum.Pointer);

            if (hasPointer && _pointerHandler == null)
            {
                _pointerHandler = new PointerHandler(_dispatcher, this);
            }
            else if (!hasPointer && _pointerHandler != null)
            {
                _pointerHandler.Dispose();
                _pointerHandler = null;
            }

            var hasTouch = capabilities.HasFlag(WlSeat.CapabilityEnum.Touch);

            if (hasTouch && _touchHandler == null)
            {
                _touchHandler = new TouchHandler(_dispatcher, this);
            }
            else if (!hasTouch && _touchHandler != null)
            {
                _touchHandler.Dispose();
                _touchHandler = null;
            }

            var hasKeyboard = capabilities.HasFlag(WlSeat.CapabilityEnum.Keyboard);

            if (hasKeyboard && _keyboardHandler == null)
            {
                _keyboardHandler = new KeyboardHandler(this);
            }
            else if (!hasKeyboard && _keyboardHandler != null)
            {
                _keyboardHandler.Dispose();
                _keyboardHandler = null;
            }
        }

        public void Dispose()
        {
            _pointerHandler?.Dispose();
            _pointerHandler = null;
            _touchHandler?.Dispose();
            _touchHandler = null;
            _keyboardHandler?.Dispose();
            _keyboardHandler = null;
            DataDevice?.Dispose();
            _wlSeat.Dispose();
        }

        internal void NotifyCursorChanged(WSurface surface)
        {
            _pointerHandler?.NotifyCursorChanged(surface);
        }

        internal void SetDndCursor(StandardCursorType cursorType)
        {
            _pointerHandler?.SetDndCursor(cursorType);
        }

        class SeatListener(Seat seat) : WlSeat.Listener
        {
            protected override void Capabilities(WlSeat eventSender, WlSeat.CapabilityEnum capabilities)
            {
                seat.OnCapabilities(capabilities);
            }
        }
    }

    class PointerHandler : IDisposable
    {
        private readonly WaylandInputDispatcher _dispatcher;
        private readonly Seat _seat;
        private readonly WlPointer _pointer;

        // Persistent pointer state (survives across frames)
        private WSurfaceEventSinkProxy? _focusedSink;
        private WSurface? _focusedSurface;
        internal WlSurface? _focusedWlSurface;
        private Point _pointerPosition;
        private RawInputModifiers _modifiers;
        private uint _lastEnterSerial;

        // Per-frame: ordered event actions dispatched during Frame
        private readonly List<Action> _frameActions = new();

        // Per-frame: axis accumulation (protocol mandates combining within a frame).
        //
        // Wayland sends two related events for scrolling:
        //  - axis_value120 (since v8) — discrete-step indicator: 120 == one wheel detent.
        //    Replaces the deprecated axis_discrete in v8+. Sent for wheel sources.
        //  - axis — continuous magnitude in surface-local pixel-equivalent units. Sent for
        //    every scroll source (wheel + touchpad/continuous), regardless of version.
        //
        // For Avalonia (1.0 == one detent), value120/120 is the right unit when present.
        // For continuous sources (touchpad), there is no value120 and we fall back to the
        // raw axis value. We accumulate the two streams independently and combine at frame
        // flush time, preferring v120 per axis when present). This is robust to either event order.
        private bool _frameHasAxis;
        private ulong _frameAxisTimestamp;
        private double _frameAxisRawX;
        private double _frameAxisRawY;
        private double _frameV120X;
        private double _frameV120Y;
        private bool _frameV120SeenX;
        private bool _frameV120SeenY;

        public PointerHandler(WaylandInputDispatcher dispatcher, Seat seat)
        {
            _dispatcher = dispatcher;
            _seat = seat;
            _pointer = seat.WlSeat.GetPointer(new Listener(this));
        }

        private void UpdateCursor()
        {
            if (_focusedSurface == null)
                return;
            var cursorInfo = _dispatcher._globals.CursorManager.GetCursor(_focusedSurface.CurrentCursor);
            if (cursorInfo is { } c)
                _pointer.SetCursor(_lastEnterSerial, c.Surface, c.HotspotX, c.HotspotY);
            else
                _pointer.SetCursor(_lastEnterSerial, null, 0, 0);
        }

        internal void NotifyCursorChanged(WSurface surface)
        {
            if (ReferenceEquals(_focusedSurface, surface))
                UpdateCursor();
        }

        /// <summary>
        /// Sets the pointer cursor to reflect the current DnD action.
        /// Called from <see cref="WaylandDataSource.OnAction"/> during an active drag source operation.
        /// </summary>
        /// <remarks>
        /// TODO: Weston does not honor <c>wl_pointer.set_cursor</c> during drag.
        /// To support Weston, we'd need to attach cursor buffers to the drag icon surface
        /// passed to <c>wl_data_device.start_drag</c>. Our current approach matches Qt behavior
        /// and works correctly with KWin/Mutter. Weston compat is low priority.
        /// </remarks>
        internal void SetDndCursor(StandardCursorType cursorType)
        {
            var cursorInfo = _dispatcher._globals.CursorManager.GetCursor(cursorType);
            if (cursorInfo is { } c)
                _pointer.SetCursor(_lastEnterSerial, c.Surface, c.HotspotX, c.HotspotY);
            else
                _pointer.SetCursor(_lastEnterSerial, null!, 0, 0);
        }

        private void ResetFrameState()
        {
            _frameActions.Clear();
            _frameHasAxis = false;
            _frameAxisRawX = 0;
            _frameAxisRawY = 0;
            _frameV120X = 0;
            _frameV120Y = 0;
            _frameV120SeenX = false;
            _frameV120SeenY = false;
        }

        public void Dispose()
        {
            _pointer.Release();
        }

        class Listener(PointerHandler handler) : WlPointer.Listener
        {
            protected override void Enter(WlPointer eventSender, uint serial, WlSurface? surface, WlFixed surfaceX, WlFixed surfaceY)
            {
                var shellSurface = WaylandInputDispatcher.FindSurfaceForWlSurface(surface);
                var pos = new Point((double)surfaceX, (double)surfaceY);
                handler._pointerPosition = pos;
                handler._frameActions.Add(() =>
                {
                    handler._focusedSink = shellSurface?.EventSink;
                    handler._focusedSurface = shellSurface;
                    handler._focusedWlSurface = surface;
                    handler._lastEnterSerial = serial;
                    handler.UpdateCursor();
                    shellSurface?.EventSink.OnPointerEnter(0, serial, pos);
                });
            }

            protected override void Leave(WlPointer eventSender, uint serial, WlSurface? surface)
            {
                handler._frameActions.Add(() =>
                {
                    var leaveSink = handler._focusedSink;
                    handler._focusedSink = null;
                    handler._focusedSurface = null;
                    handler._focusedWlSurface = null;
                    leaveSink?.OnPointerLeave(serial);
                });
            }

            protected override void Motion(WlPointer eventSender, uint time, WlFixed surfaceX, WlFixed surfaceY)
            {
                var pos = new Point((double)surfaceX, (double)surfaceY);
                handler._pointerPosition = pos;
                var mods = handler._modifiers | handler._seat.KeyboardModifiers;
                handler._frameActions.Add(() =>
                {
                    handler._focusedSink?.OnPointerMotion(time, pos, mods);
                });
            }

            protected override void Button(WlPointer eventSender, uint serial, uint time, uint button, WlPointer.ButtonStateEnum state)
            {
                if (handler._seat.DataDevice != null)
                    handler._seat.DataDevice.LastInputSerial = serial;

                var pressed = state == WlPointer.ButtonStateEnum.Pressed;
                var type = MapButton(button, pressed);
                if (type == null)
                    return;

                var mod = ButtonToModifier(button);
                if (pressed)
                    handler._modifiers |= mod;
                else
                    handler._modifiers &= ~mod;

                var mods = handler._modifiers | handler._seat.KeyboardModifiers;
                var pos = handler._pointerPosition;
                var cookie = pressed ? new WaylandInputEventCookie(handler._seat.WlSeat, serial, handler._dispatcher._globals.Connection.Display, handler._dispatcher._globals.Worker, handler._dispatcher._globals) : null;
                handler._frameActions.Add(() =>
                {
                    handler._focusedSink?.OnPointerButton(time, serial, type.Value, mods, pos, cookie);
                });
            }

            private void EnqueueAxisDispatchIfNeeded()
            {
                if (handler._frameHasAxis)
                    return;
                handler._frameHasAxis = true;
                handler._frameActions.Add(() =>
                {
                    // Combine v120 (notches) with raw axis (continuous). Per axis, prefer v120
                    // if any v120 event fired this frame; otherwise fall back to the raw axis.
                    var deltaX = handler._frameV120SeenX ? handler._frameV120X : handler._frameAxisRawX;
                    var deltaY = handler._frameV120SeenY ? handler._frameV120Y : handler._frameAxisRawY;
                    if (deltaX != 0 || deltaY != 0)
                    {
                        // Wayland: positive = scroll down/right. Avalonia: positive Y = scroll up.
                        var delta = new Vector(-deltaX, -deltaY);
                        handler._focusedSink?.OnPointerAxis(handler._frameAxisTimestamp, delta,
                            handler._modifiers | handler._seat.KeyboardModifiers, handler._pointerPosition);
                    }
                });
            }

            protected override void Axis(WlPointer eventSender, uint time, WlPointer.AxisEnum axis, WlFixed value)
            {
                handler._frameAxisTimestamp = time;

                if (axis == WlPointer.AxisEnum.VerticalScroll)
                    handler._frameAxisRawY += (double)value;
                else if (axis == WlPointer.AxisEnum.HorizontalScroll)
                    handler._frameAxisRawX += (double)value;

                EnqueueAxisDispatchIfNeeded();
            }

            protected override void AxisSource(WlPointer eventSender, WlPointer.AxisSourceEnum axisSource)
            {
            }

            protected override void AxisStop(WlPointer eventSender, uint time, WlPointer.AxisEnum axis)
            {
            }

            protected override void AxisDiscrete(WlPointer eventSender, WlPointer.AxisEnum axis, int discrete)
            {
                // wl_pointer v5–v7 discrete-step indicator. 1 == one wheel detent. Deprecated
                // and not sent in v8+ (replaced by axis_value120). Folded into the v120
                // accumulator (since their semantics align: 1 detent each).
                if (axis == WlPointer.AxisEnum.VerticalScroll)
                {
                    handler._frameV120Y += discrete;
                    handler._frameV120SeenY = true;
                }
                else if (axis == WlPointer.AxisEnum.HorizontalScroll)
                {
                    handler._frameV120X += discrete;
                    handler._frameV120SeenX = true;
                }

                EnqueueAxisDispatchIfNeeded();
            }

            protected override void AxisValue120(WlPointer eventSender, WlPointer.AxisEnum axis, int value120)
            {
                // Higher-resolution scroll: 120 units = one notch. Multiple value120 events
                // for the same axis in a frame must be summed (per spec example: a single
                // hardware event can produce -240 = two negative notches).
                if (axis == WlPointer.AxisEnum.VerticalScroll)
                {
                    handler._frameV120Y += value120 / 120.0;
                    handler._frameV120SeenY = true;
                }
                else if (axis == WlPointer.AxisEnum.HorizontalScroll)
                {
                    handler._frameV120X += value120 / 120.0;
                    handler._frameV120SeenX = true;
                }

                EnqueueAxisDispatchIfNeeded();
            }

            protected override void Frame(WlPointer eventSender)
            {
                try
                {
                    foreach (var action in handler._frameActions)
                        action();
                }
                finally
                {
                    handler.ResetFrameState();
                }
            }
        }
    }

    class TouchHandler : IDisposable
    {
        private readonly WaylandInputDispatcher _dispatcher;
        private readonly Seat _seat;
        private readonly WlTouch _touch;

        // Track active touch point ID → (surface sink, last position)
        private readonly Dictionary<int, (WSurfaceEventSinkProxy Sink, Point Position)> _activeTouchPoints = new();

        // Per-frame: ordered event actions dispatched during Frame
        private readonly List<Action> _frameActions = new();

        public TouchHandler(WaylandInputDispatcher dispatcher, Seat seat)
        {
            _dispatcher = dispatcher;
            _seat = seat;
            _touch = seat.WlSeat.GetTouch(new Listener(this));
        }

        private void ResetFrameState()
        {
            _frameActions.Clear();
        }

        public void Dispose()
        {
            _touch.Release();
        }

        class Listener(TouchHandler handler) : WlTouch.Listener
        {
            protected override void Down(WlTouch eventSender, uint serial, uint time, WlSurface? surface,
                int id, WlFixed x, WlFixed y)
            {
                if (handler._seat.DataDevice != null)
                    handler._seat.DataDevice.LastInputSerial = serial;

                var shellSurface = WaylandInputDispatcher.FindSurfaceForWlSurface(surface);
                var pos = new Point((double)x, (double)y);
                var cookie = new WaylandInputEventCookie(handler._seat.WlSeat, serial, handler._dispatcher._globals.Connection.Display, handler._dispatcher._globals.Worker, handler._dispatcher._globals);
                handler._frameActions.Add(() =>
                {
                    if (shellSurface != null)
                    {
                        handler._activeTouchPoints[id] = (shellSurface.EventSink, pos);
                        shellSurface.EventSink.OnTouchDown(time, id, pos, cookie);
                    }
                });
            }

            protected override void Motion(WlTouch eventSender, uint time, int id, WlFixed x, WlFixed y)
            {
                var pos = new Point((double)x, (double)y);
                handler._frameActions.Add(() =>
                {
                    if (handler._activeTouchPoints.TryGetValue(id, out var entry))
                    {
                        entry.Position = pos;
                        handler._activeTouchPoints[id] = entry;
                        entry.Sink.OnTouchMove(time, id, pos);
                    }
                });
            }

            protected override void Up(WlTouch eventSender, uint serial, uint time, int id)
            {
                handler._frameActions.Add(() =>
                {
                    if (handler._activeTouchPoints.Remove(id, out var entry))
                        entry.Sink.OnTouchUp(time, id, entry.Position);
                });
            }

            protected override void Cancel(WlTouch eventSender)
            {
                handler._frameActions.Add(() =>
                {
                    foreach (var (id, entry) in handler._activeTouchPoints)
                        entry.Sink.OnTouchCancel(id, entry.Position);
                    handler._activeTouchPoints.Clear();
                });
            }

            protected override void Frame(WlTouch eventSender)
            {
                try
                {
                    foreach (var action in handler._frameActions)
                        action();
                }
                finally
                {
                    handler.ResetFrameState();
                }
            }

            protected override void Shape(WlTouch eventSender, int id, WlFixed major, WlFixed minor)
            {
                // Shape info not currently used
            }

            protected override void Orientation(WlTouch eventSender, int id, WlFixed orientation)
            {
                // Orientation info not currently used
            }
        }
    }

}
