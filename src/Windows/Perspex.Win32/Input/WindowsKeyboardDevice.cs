// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Text;
using Perspex.Controls;
using Perspex.Input;
using Perspex.Win32.Interop;

namespace Perspex.Win32.Input
{
    public class WindowsKeyboardDevice : KeyboardDevice
    {
        private static WindowsKeyboardDevice s_instance = new WindowsKeyboardDevice();

        private byte[] _keyStates = new byte[256];

        public static new WindowsKeyboardDevice Instance
        {
            get { return s_instance; }
        }

        public ModifierKeys Modifiers
        {
            get
            {
                UpdateKeyStates();
                ModifierKeys result = 0;

                if (IsDown(Key.LeftAlt) || IsDown(Key.RightAlt))
                {
                    result |= ModifierKeys.Alt;
                }

                if (IsDown(Key.LeftCtrl) || IsDown(Key.RightCtrl))
                {
                    result |= ModifierKeys.Control;
                }

                if (IsDown(Key.LeftShift) || IsDown(Key.RightShift))
                {
                    result |= ModifierKeys.Shift;
                }

                if (IsDown(Key.LWin) || IsDown(Key.RWin))
                {
                    result |= ModifierKeys.Windows;
                }

                return result;
            }
        }

        public void WindowActivated(Window window)
        {
            SetFocusedElement(window, NavigationMethod.Unspecified);
        }

        public string StringFromVirtualKey(uint virtualKey)
        {
            StringBuilder result = new StringBuilder(256);
            int length = UnmanagedMethods.ToUnicode(
                virtualKey,
                0,
                _keyStates,
                result,
                256,
                0);
            return result.ToString();
        }

        private void UpdateKeyStates()
        {
            UnmanagedMethods.GetKeyboardState(_keyStates);
        }

        private bool IsDown(Key key)
        {
            return (GetKeyStates(key) & KeyStates.Down) != 0;
        }

        private KeyStates GetKeyStates(Key key)
        {
            int vk = KeyInterop.VirtualKeyFromKey(key);
            byte state = _keyStates[vk];
            KeyStates result = 0;

            if ((state & 0x80) != 0)
            {
                result |= KeyStates.Down;
            }

            if ((state & 0x01) != 0)
            {
                result |= KeyStates.Toggled;
            }

            return result;
        }
    }
}
