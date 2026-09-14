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

        // Standard XKB modifier bit positions
        private const uint ModShift = 1 << 0;
        private const uint ModControl = 1 << 2;
        private const uint ModAlt = 1 << 3;    // Mod1
        private const uint ModMeta = 1 << 6;   // Mod4 / Super

        public KeyboardHandler(Seat seat)
        {
            _seat = seat;
            _xkbContext = new XkbContext();
            _composeTable = XkbComposeTable.TryCreate(_xkbContext);

            _keyboard = seat.WlSeat.GetKeyboard(new Listener(this));
        }

        private static RawInputModifiers ModifiersFromXkbMask(uint mask)
        {
            var mods = RawInputModifiers.None;
            if ((mask & ModShift) != 0) mods |= RawInputModifiers.Shift;
            if ((mask & ModControl) != 0) mods |= RawInputModifiers.Control;
            if ((mask & ModAlt) != 0) mods |= RawInputModifiers.Alt;
            if ((mask & ModMeta) != 0) mods |= RawInputModifiers.Meta;
            return mods;
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
            }

            protected override void Key(WlKeyboard eventSender, uint serial, uint time, uint key, WlKeyboard.KeyStateEnum state)
            {
                if (handler._seat.DataDevice != null)
                    handler._seat.DataDevice.LastInputSerial = serial;

                var sink = handler._focusedSurface?.EventSink;
                if (sink == null)
                    return;

                var physicalKey = XkbKeyTransform.PhysicalKeyFromEvdev(key);
                var mods = handler._seat.KeyboardModifiers;

                Key avKey;
                string? keySymbol;

                if (handler._keymap != null)
                {
                    (avKey, keySymbol) = XkbKeyTransform.ResolveKeyWithFallback(
                        handler._keymap, key, physicalKey);

                    // Process compose sequences on key press only, and only when the
                    // focused surface has an active text input client (e.g. a TextBox)
                    if (state == WlKeyboard.KeyStateEnum.Pressed
                        && handler._composeState != null
                        && handler._focusedSurface is { HasTextInputClient: true })
                    {
                        var (resolvedKeysym, _) = handler._keymap.ResolveKey(key);
                        var composeStatus = handler._composeState.Feed(resolvedKeysym);

                        switch (composeStatus)
                        {
                            case XKB_COMPOSE_COMPOSING:
                                // In the middle of a compose sequence — suppress entire event
                                return;
                            case XKB_COMPOSE_COMPOSED:
                                // Compose sequence complete — use composed text
                                keySymbol = XkbKeyTransform.FilterKeySymbol(
                                    handler._composeState.GetComposedText());
                                var composedKeysym = handler._composeState.GetComposedKeysym();
                                var composedKey = XkbKeyTransform.KeyFromKeysym(composedKeysym);
                                if (composedKey != Avalonia.Input.Key.None)
                                    avKey = composedKey;
                                break;
                            case XKB_COMPOSE_CANCELLED:
                                // Sequence cancelled — normal processing of current key
                                break;
                            // XKB_COMPOSE_NOTHING — normal processing
                        }
                    }
                }
                else
                {
                    avKey = XkbKeyTransform.KeyFromEvdev(key);
                    keySymbol = null;
                }

                if (state == WlKeyboard.KeyStateEnum.Pressed)
                    sink.OnKeyDown(time, avKey, mods, physicalKey, keySymbol);
                else
                    sink.OnKeyUp(time, avKey, mods, physicalKey, keySymbol);
            }

            protected override void Modifiers(WlKeyboard eventSender, uint serial, uint modsDepressed, uint modsLatched, uint modsLocked, uint group)
            {
                handler._keymap?.UpdateModifiers(modsDepressed, modsLatched, modsLocked, group);

                var effective = modsDepressed | modsLatched | modsLocked;
                handler._seat.KeyboardModifiers = ModifiersFromXkbMask(effective);
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
