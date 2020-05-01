// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Input;

namespace Avalonia.Controls.Utils
{
    internal static class KeyboardHelper
    {
        public static void GetMetaKeyState(KeyModifiers modifiers, out bool ctrl, out bool shift)
        {
            ctrl = (modifiers & KeyModifiers.Control) == KeyModifiers.Control;
            shift = (modifiers & KeyModifiers.Shift) == KeyModifiers.Shift;
        }

        public static void GetMetaKeyState(KeyModifiers modifiers, out bool ctrl, out bool shift, out bool alt)
        {
            ctrl = (modifiers & KeyModifiers.Control) == KeyModifiers.Control;
            shift = (modifiers & KeyModifiers.Shift) == KeyModifiers.Shift;
            alt = (modifiers & KeyModifiers.Alt) == KeyModifiers.Alt;
        }
    }
}
