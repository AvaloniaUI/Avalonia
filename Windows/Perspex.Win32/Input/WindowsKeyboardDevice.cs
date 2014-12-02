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

        public static WindowsKeyboardDevice Instance
        {
            get { return instance; }
        }

        public override ModifierKeys Modifiers
        {
            get
            {
                ModifierKeys result = 0;

                if (this.GetKeyStates(Key.LeftAlt) == KeyStates.Down ||
                    this.GetKeyStates(Key.RightAlt) == KeyStates.Down)
                {
                    result |= ModifierKeys.Alt;
                }

                if (this.GetKeyStates(Key.LeftCtrl) == KeyStates.Down ||
                    this.GetKeyStates(Key.RightCtrl) == KeyStates.Down)
                {
                    result |= ModifierKeys.Control;
                }

                if (this.GetKeyStates(Key.LeftShift) == KeyStates.Down ||
                    this.GetKeyStates(Key.RightShift) == KeyStates.Down)
                {
                    result |= ModifierKeys.Shift;
                }

                if (this.GetKeyStates(Key.LWin) == KeyStates.Down ||
                    this.GetKeyStates(Key.RWin) == KeyStates.Down)
                {
                    result |= ModifierKeys.Windows;
                }

                return result;
            }
        }

        public void WindowActivated(Window window)
        {
            this.FocusedElement = window;
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
