using System;
using System.Linq;
using Avalonia.Input;
using Avalonia.Input.Raw;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputKeyCodes;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods.LibInputKeyState;

namespace Avalonia.LinuxFramebuffer.Input.LibInput
{
    
    public partial class LibInputBackend
    {
        private InputModifiers _kbdState = InputModifiers.None;
        
        private void HandleKey(IntPtr ev, LibInputEventType type)
        {
            var ts = libinput_event_keyboard_get_time_usec(ev) / 1000;
            var state = libinput_event_keyboard_get_key_state(ev);
            var keycode = libinput_event_keyboard_get_key(ev);
            Key key;
            if (keycode == KEY_LEFTALT || keycode == KEY_RIGHTALT)
            {
                key = keycode == KEY_LEFTALT ? Key.LeftAlt : Key.RightAlt;
                
                if (state == LIBINPUT_KEY_STATE_PRESSED)
                    _kbdState |= InputModifiers.Alt;
                else
                    _kbdState &= ~InputModifiers.Alt;
            }
            else if (keycode == KEY_LEFTCTRL || keycode == KEY_RIGHTCTRL)
            {
                key = keycode == KEY_LEFTCTRL ? Key.LeftCtrl : Key.RightCtrl;
                if (state == LIBINPUT_KEY_STATE_PRESSED)
                    _kbdState |= InputModifiers.Control;
                else
                    _kbdState &= ~InputModifiers.Control;
            }
            else if (keycode == KEY_LEFTMETA || keycode == KEY_RIGHTMETA)
            {
                key = keycode == KEY_LEFTMETA ? Key.LWin : Key.RWin;
                if (state == LIBINPUT_KEY_STATE_PRESSED)
                    _kbdState |= InputModifiers.Windows;
                else
                    _kbdState &= ~InputModifiers.Windows;
            }
            else if (keycode == KEY_LEFTSHIFT || keycode == KEY_RIGHTSHIFT)
            {
                key = keycode == KEY_LEFTSHIFT ? Key.LeftShift : Key.RightShift;
                if (state == LIBINPUT_KEY_STATE_PRESSED)
                    _kbdState |= InputModifiers.Shift;
                else
                    _kbdState &= ~InputModifiers.Shift;
            }
            else
            {
                key = KeyLookup[keycode].FirstOrDefault();
            }

            ScheduleInput(new RawKeyEventArgs(_kbd, ts, (RawKeyEventType)state, key, _kbdState));
        }
    }
}
