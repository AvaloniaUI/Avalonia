// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Input;

namespace Avalonia.Controls.Utils
{
    internal static class KeyboardHelper
    {
        public static void GetMetaKeyState(InputModifiers modifiers, out bool ctrl, out bool shift)
        {
            ctrl = (modifiers & InputModifiers.Control) == InputModifiers.Control;
            shift = (modifiers & InputModifiers.Shift) == InputModifiers.Shift;
        }
        public static void GetMetaKeyState(InputModifiers modifiers, out bool ctrl, out bool shift, out bool alt)
        {
            ctrl = (modifiers & InputModifiers.Control) == InputModifiers.Control;
            shift = (modifiers & InputModifiers.Shift) == InputModifiers.Shift;
            alt = (modifiers & InputModifiers.Alt) == InputModifiers.Alt;
        }
    }
}
