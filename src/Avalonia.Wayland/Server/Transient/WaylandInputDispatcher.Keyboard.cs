using System;
using Avalonia.Input;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Persistent;
using NWayland;
using NWayland.Protocols.Wayland;
using static Avalonia.Wayland.Server.Interop.XkbCommonNativeMethods;

namespace Avalonia.Wayland.Server.Transient;

partial class WaylandInputDispatcher
{
    class KeyboardHandler : IDisposable
    {
        private readonly Seat _seat;
        private readonly WlKeyboard _keyboard;

        private readonly XkbContext _xkbContext;
        private readonly XkbComposeTable? _composeTable;

        private WXdgShellSurface? _focusedSurface;
        private int _repeatRate;  // keys/sec (0 = disabled)
        private int _repeatDelay; // ms before first repeat
        private XkbCommonKeymap? _keymap;
        private XkbComposeState? _composeState;
        private uint _effectiveMods;
        private uint? _heldKey;


        public KeyboardHandler(Seat seat)
        {
            _seat = seat;
            _xkbContext = new XkbContext();
            _composeTable = XkbComposeTable.TryCreate(_xkbContext);

            _keyboard = seat.WlSeat.GetKeyboard(new Listener(this));
        }


        public void Dispose()
        {
            _keyboard.Release();
            _composeState?.Dispose();
            _composeState = null;
            _keymap?.Dispose();
            _keymap = null;
            _composeTable?.Dispose();
            _xkbContext.Dispose();
        }

        /// <summary>
        /// Resolves an evdev key code against the current xkb state. With <paramref name="compose"/>, the key also
        /// runs through the compose state, but only when the focused surface has an active text input client
        /// (e.g. a TextBox). Returns null when a compose sequence is in progress and the event must be suppressed.
        /// </summary>
        private WaylandKeyInfo? ResolveKey(uint key, bool press, bool compose)
        {
            var physicalKey = XkbKeyTransform.PhysicalKeyFromEvdev(key);
            if (_keymap == null)
                return new WaylandKeyInfo(XkbKeyTransform.KeyFromEvdev(key), physicalKey, null, null, null, null,
                    new NativeKeyInfo
                    {
                        Format = NativeKeyInfoFormat.Wayland,
                        ScanCode = key + 8,
                        Modifiers = _effectiveMods
                    });

            var (avKey, keySymbol, text, keysym) = XkbKeyTransform.ResolveKeyWithFallback(_keymap, key, physicalKey);

            if (compose
                && _composeState != null
                && _focusedSurface is { HasTextInputClient: true })
            {
                switch (_composeState.Feed(keysym))
                {
                    case XKB_COMPOSE_COMPOSING:
                        // In the middle of a compose sequence — suppress entire event
                        return null;
                    case XKB_COMPOSE_COMPOSED:
                        // Compose sequence complete — use composed text
                        text = _composeState.GetComposedText();
                        keySymbol = XkbKeyTransform.FilterKeySymbol(text);
                        var composedKey = XkbKeyTransform.KeyFromKeysym(_composeState.GetComposedKeysym());
                        if (composedKey != Key.None)
                            avKey = composedKey;
                        break;
                    // XKB_COMPOSE_CANCELLED — sequence cancelled, normal processing of current key
                    // XKB_COMPOSE_NOTHING — normal processing
                }
            }

            return new WaylandKeyInfo(avKey, physicalKey, keySymbol,
                press ? text : null,
                press ? XkbCommonKeymap.KeysymToText(keysym) : null,
                // Only pressed modifiers count; the GTK mode already reports consumed modifiers that are active
                press ? XkbKeyTransform.ModifiersFromXkbMask(_keymap.GetConsumedModifiers(key) & _effectiveMods) : null,
                new NativeKeyInfo
                {
                    Format = NativeKeyInfoFormat.Wayland,
                    ScanCode = key + 8,
                    VirtualKey = keysym != 0 ? keysym : null,
                    Modifiers = _effectiveMods
                });
        }

        class Listener(KeyboardHandler handler) : WlKeyboard.Listener
        {
            protected override void Keymap(WlKeyboard eventSender, WlKeyboard.KeymapFormatEnum format, WaylandFd fd, uint size)
            {
                handler._composeState?.Dispose();
                handler._composeState = null;
                handler._keymap?.Dispose();
                handler._keymap = null;

                var rawFd = fd.Consume();
                if (format == WlKeyboard.KeymapFormatEnum.XkbV1)
                {
                    try
                    {
                        handler._keymap = new XkbCommonKeymap(handler._xkbContext, rawFd, size);

                        if (handler._composeTable != null)
                            handler._composeState = new XkbComposeState(handler._composeTable);
                    }
                    catch
                    {
                        // If keymap creation fails, fall back to evdev-only mapping
                    }
                }
                else
                {
                    UnsafeNativeMethods.close(rawFd);
                }
            }

            protected override void Enter(WlKeyboard eventSender, uint serial, WlSurface? surface, ReadOnlySpan<byte> keys)
            {
                handler._focusedSurface = WaylandInputDispatcher.FindSurfaceForWlSurface(surface);
                handler._focusedSurface?.EventSink.OnKeyRepeatInfo(handler._repeatRate, handler._repeatDelay);

                if (handler._seat.DataDevice != null)
                    handler._seat.DataDevice.LastInputSerial = serial;
            }

            protected override void Leave(WlKeyboard eventSender, uint serial, WlSurface? surface)
            {
                handler._composeState?.Reset();
                handler._focusedSurface?.EventSink.OnKeyboardLeave();
                handler._focusedSurface = null;
                handler._seat.KeyboardModifiers = RawInputModifiers.None;
                handler._effectiveMods = 0;
                handler._heldKey = null;
            }

            protected override void Key(WlKeyboard eventSender, uint serial, uint time, uint key, WlKeyboard.KeyStateEnum state)
            {
                if (handler._seat.DataDevice != null)
                    handler._seat.DataDevice.LastInputSerial = serial;

                var sink = handler._focusedSurface?.EventSink;
                if (sink == null)
                    return;

                var mods = handler._seat.KeyboardModifiers;
                var pressed = state == WlKeyboard.KeyStateEnum.Pressed;
                var info = handler.ResolveKey(key, pressed, pressed);
                if (info == null)
                    return;

                if (pressed)
                {
                    handler._heldKey = key;
                    sink.OnKeyDown(time, mods, info);
                }
                else
                {
                    if (handler._heldKey == key)
                        handler._heldKey = null;
                    sink.OnKeyUp(time, mods, info);
                }
            }

            protected override void Modifiers(WlKeyboard eventSender, uint serial, uint modsDepressed, uint modsLatched, uint modsLocked, uint group)
            {
                handler._keymap?.UpdateModifiers(modsDepressed, modsLatched, modsLocked, group);

                var effective = modsDepressed | modsLatched | modsLocked;
                handler._seat.KeyboardModifiers = XkbKeyTransform.ModifiersFromXkbMask(effective);
                handler._effectiveMods = effective;

                if (handler._heldKey is { } heldKey
                    && handler._focusedSurface?.EventSink is { } sink
                    && handler.ResolveKey(heldKey, true, false) is { } info)
                    sink.OnHeldKeyChanged(handler._seat.KeyboardModifiers, info);
            }

            protected override void RepeatInfo(WlKeyboard eventSender, int rate, int delay)
            {
                handler._repeatRate = rate;
                handler._repeatDelay = delay;
                handler._focusedSurface?.EventSink.OnKeyRepeatInfo(rate, delay);
            }
        }
    }
}
