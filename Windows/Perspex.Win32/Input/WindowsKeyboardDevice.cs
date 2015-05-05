// -----------------------------------------------------------------------
// <copyright file="WindowsKeyboardDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Win32.Input
{
    using System.Text;
    using Perspex.Controls;
    using Perspex.Input;
    using Perspex.Win32.Interop;

    public class WindowsKeyboardDevice : KeyboardDevice
    {
        private static WindowsKeyboardDevice instance = new WindowsKeyboardDevice();

        private byte[] keyStates = new byte[256];

        public static new WindowsKeyboardDevice Instance
        {
            get { return instance; }
        }

        public override ModifierKeys Modifiers
        {
            get
            {
                ModifierKeys result = 0;

                if (this.IsDown(Key.LeftAlt) || this.IsDown(Key.RightAlt))
                {
                    result |= ModifierKeys.Alt;
                }

                if (this.IsDown(Key.LeftCtrl) || this.IsDown(Key.RightCtrl))
                {
                    result |= ModifierKeys.Control;
                }

                if (this.IsDown(Key.LeftShift) || this.IsDown(Key.RightShift))
                {
                    result |= ModifierKeys.Shift;
                }

                if (this.IsDown(Key.LWin) || this.IsDown(Key.RWin))
                {
                    result |= ModifierKeys.Windows;
                }

                return result;
            }
        }

        public void WindowActivated(Window window)
        {
            this.SetFocusedElement(window, false);
        }

        public string StringFromVirtualKey(uint virtualKey)
        {
            StringBuilder result = new StringBuilder(256);
            int length = UnmanagedMethods.ToUnicode(
                virtualKey,
                0,
                this.keyStates,
                result,
                256,
                0);
            return result.ToString();
        }

        internal void UpdateKeyStates()
        {
            UnmanagedMethods.GetKeyboardState(this.keyStates);
        }

        private bool IsDown(Key key)
        {
            return (this.GetKeyStates(key) & KeyStates.Down) != 0;
        }

        private KeyStates GetKeyStates(Key key)
        {
            int vk = KeyInterop.VirtualKeyFromKey(key);
            byte state = this.keyStates[vk];
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
