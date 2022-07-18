using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods;

namespace Avalonia.LinuxFramebuffer.Input.LibInput
{
    public partial class LibInputBackend
    {
        private readonly KeyboardDevice _keyboard = new();
        private readonly HashSet<libinput_key> _pressedKeys = new();

        private void HandleKeyboardEvent(IntPtr rawEvent, LibInputEventType type)
        {
            var rawKeyboardEvent = libinput_event_get_keyboard_event(rawEvent);

            if (rawKeyboardEvent != IntPtr.Zero)
            {
                var key = libinput_event_keyboard_get_key(rawKeyboardEvent);
                var state = libinput_event_keyboard_get_key_state(rawKeyboardEvent);
                var timestamp = (ulong)libinput_event_keyboard_get_time(rawKeyboardEvent);

                if (state == libinput_key_state.Pressed)
                {
                    OnKeyPressEvent(key, timestamp);
                }
                else if (state == libinput_key_state.Released)
                {
                    OnKeyReleaseEvent(key, timestamp);
                }
                else
                {
                    if (Logging.Logger.IsEnabled(Logging.LogEventLevel.Warning, LibInput))
                    {
                        Logging.Logger.TryGet(Logging.LogEventLevel.Warning, LibInput)
                            ?.Log(this, $"{nameof(HandleKeyboardEvent)}: Unsupported state {state} for {key}");
                    }
                }
            }
        }

        private void OnKeyPressEvent(libinput_key rawKey, ulong timestamp)
        {
            var key = ConvertToVirtualKey(rawKey);

            if (Logging.Logger.IsEnabled(Logging.LogEventLevel.Debug, LibInput))
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Debug, LibInput)
                    ?.Log(this, ($"{nameof(OnKeyPressEvent)}: {rawKey} -> {key}"));
            }

            _pressedKeys.Add(rawKey);

            var args = new RawKeyEventArgs(_keyboard
                , timestamp
                , _inputRoot
                , RawKeyEventType.KeyDown
                , key
                , GetCurrentModifiersState());

            ScheduleInput(args);
        }

        private void OnKeyReleaseEvent(libinput_key rawKey, ulong timestamp)
        {
            var key = ConvertToVirtualKey(rawKey);

            if (Logging.Logger.IsEnabled(Logging.LogEventLevel.Debug, LibInput))
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Debug, LibInput)
                    ?.Log(this, ($"{nameof(OnKeyReleaseEvent)}: {rawKey} -> {key}"));
            }

            _pressedKeys.Add(rawKey);

            var args = new RawKeyEventArgs(_keyboard
                , timestamp
                , _inputRoot
                , RawKeyEventType.KeyUp
                , key
                , GetCurrentModifiersState());

            ScheduleInput(args);
        }

        private RawInputModifiers GetCurrentModifiersState()
        {
            var modifiers = RawInputModifiers.None;

            if (_pressedKeys.Contains(libinput_key.KEY_LEFTSHIFT)
                || _pressedKeys.Contains(libinput_key.KEY_RIGHTSHIFT))
            {
                modifiers |= RawInputModifiers.Shift;
            }
            if (_pressedKeys.Contains(libinput_key.KEY_LEFTCTRL)
                || _pressedKeys.Contains(libinput_key.KEY_RIGHTCTRL))
            {
                modifiers |= RawInputModifiers.Control;
            }
            if (_pressedKeys.Contains(libinput_key.KEY_LEFTALT)
                || _pressedKeys.Contains(libinput_key.KEY_RIGHTALT))
            {
                modifiers |= RawInputModifiers.Alt;
            }

            return modifiers;
        }

        private Key ConvertToVirtualKey(libinput_key key) =>
            key switch
            {
                libinput_key.KEY_RESERVED => Key.None,
                libinput_key.KEY_ESC => Key.Escape,
                libinput_key.KEY_1 => Key.D1,
                libinput_key.KEY_2 => Key.D2,
                libinput_key.KEY_3 => Key.D3,
                libinput_key.KEY_4 => Key.D4,
                libinput_key.KEY_5 => Key.D5,
                libinput_key.KEY_6 => Key.D6,
                libinput_key.KEY_7 => Key.D7,
                libinput_key.KEY_8 => Key.D8,
                libinput_key.KEY_9 => Key.D9,
                libinput_key.KEY_0 => Key.D0,
                libinput_key.KEY_MINUS => Key.OemMinus,
                libinput_key.KEY_BACKSPACE => Key.Back,
                libinput_key.KEY_TAB => Key.Tab,
                libinput_key.KEY_Q => Key.Q,
                libinput_key.KEY_W => Key.W,
                libinput_key.KEY_E => Key.E,
                libinput_key.KEY_R => Key.R,
                libinput_key.KEY_T => Key.T,
                libinput_key.KEY_Y => Key.Y,
                libinput_key.KEY_U => Key.U,
                libinput_key.KEY_I => Key.I,
                libinput_key.KEY_O => Key.O,
                libinput_key.KEY_P => Key.P,
                libinput_key.KEY_ENTER => Key.Enter,
                libinput_key.KEY_LEFTCTRL => Key.LeftCtrl,
                libinput_key.KEY_A => Key.A,
                libinput_key.KEY_S => Key.S,
                libinput_key.KEY_D => Key.D,
                libinput_key.KEY_F => Key.F,
                libinput_key.KEY_G => Key.G,
                libinput_key.KEY_H => Key.H,
                libinput_key.KEY_J => Key.J,
                libinput_key.KEY_K => Key.K,
                libinput_key.KEY_L => Key.L,
                libinput_key.KEY_SEMICOLON => Key.OemSemicolon,
                libinput_key.KEY_LEFTSHIFT => Key.LeftShift,
                libinput_key.KEY_BACKSLASH => Key.OemBackslash,
                libinput_key.KEY_Z => Key.Z,
                libinput_key.KEY_X => Key.X,
                libinput_key.KEY_C => Key.C,
                libinput_key.KEY_V => Key.V,
                libinput_key.KEY_B => Key.B,
                libinput_key.KEY_N => Key.N,
                libinput_key.KEY_M => Key.M,
                libinput_key.KEY_COMMA => Key.OemComma,
                libinput_key.KEY_RIGHTSHIFT => Key.RightShift,
                libinput_key.KEY_SPACE => Key.Space,
                libinput_key.KEY_CAPSLOCK => Key.CapsLock,
                libinput_key.KEY_F1 => Key.F1,
                libinput_key.KEY_F2 => Key.F2,
                libinput_key.KEY_F3 => Key.F3,
                libinput_key.KEY_F4 => Key.F4,
                libinput_key.KEY_F5 => Key.F5,
                libinput_key.KEY_F6 => Key.F6,
                libinput_key.KEY_F7 => Key.F7,
                libinput_key.KEY_F8 => Key.F8,
                libinput_key.KEY_F9 => Key.F9,
                libinput_key.KEY_F10 => Key.F10,
                libinput_key.KEY_NUMLOCK => Key.NumLock,
                libinput_key.KEY_SCROLLLOCK => Key.Scroll,
                libinput_key.KEY_KP7 => Key.NumPad7,
                libinput_key.KEY_KP8 => Key.NumPad8,
                libinput_key.KEY_KP9 => Key.NumPad9,
                libinput_key.KEY_KP4 => Key.NumPad4,
                libinput_key.KEY_KP5 => Key.NumPad5,
                libinput_key.KEY_KP6 => Key.NumPad6,
                libinput_key.KEY_KP1 => Key.NumPad1,
                libinput_key.KEY_KP2 => Key.NumPad2,
                libinput_key.KEY_KP3 => Key.NumPad3,
                libinput_key.KEY_KP0 => Key.NumPad0,
                libinput_key.KEY_F11 => Key.F11,
                libinput_key.KEY_F12 => Key.F12,
                libinput_key.KEY_RIGHTCTRL => Key.RightCtrl,
                libinput_key.KEY_RIGHTALT => Key.RightAlt,
                libinput_key.KEY_LINEFEED => Key.LineFeed,
                libinput_key.KEY_HOME => Key.Home,
                libinput_key.KEY_UP => Key.Up,
                libinput_key.KEY_PAGEUP => Key.PageUp,
                libinput_key.KEY_LEFT => Key.Left,
                libinput_key.KEY_RIGHT => Key.Right,
                libinput_key.KEY_END => Key.End,
                libinput_key.KEY_DOWN => Key.Down,
                libinput_key.KEY_PAGEDOWN => Key.PageDown,
                libinput_key.KEY_INSERT => Key.Insert,
                libinput_key.KEY_DELETE => Key.Delete,
                libinput_key.KEY_MUTE => Key.VolumeMute,
                libinput_key.KEY_VOLUMEDOWN => Key.VolumeDown,
                libinput_key.KEY_VOLUMEUP => Key.VolumeUp,
                libinput_key.KEY_PAUSE => Key.Pause,
                libinput_key.KEY_STOP => Key.MediaStop,
                libinput_key.KEY_F13 => Key.F13,
                libinput_key.KEY_F14 => Key.F14,
                libinput_key.KEY_F15 => Key.F15,
                libinput_key.KEY_F16 => Key.F16,
                libinput_key.KEY_F17 => Key.F17,
                libinput_key.KEY_F18 => Key.F18,
                libinput_key.KEY_F19 => Key.F19,
                libinput_key.KEY_F20 => Key.F20,
                libinput_key.KEY_F21 => Key.F21,
                libinput_key.KEY_F22 => Key.F22,
                libinput_key.KEY_F23 => Key.F23,
                libinput_key.KEY_F24 => Key.F24,
                libinput_key.KEY_PLAYCD => Key.MediaPlayPause,
                libinput_key.KEY_PAUSECD => Key.MediaPlayPause,
                libinput_key.KEY_PLAY => Key.MediaPlayPause,
                libinput_key.KEY_PRINT => Key.Print,
                libinput_key.KEY_QUESTION => Key.OemQuestion,
                libinput_key.KEY_EMAIL => Key.LaunchMail,
                _ => Key.None
            };
    }
}
