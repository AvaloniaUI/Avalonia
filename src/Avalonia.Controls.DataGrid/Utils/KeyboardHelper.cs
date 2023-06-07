// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Controls.Utils
{
    internal static class KeyboardHelper
    {
        public static void GetMetaKeyState(Control target, KeyModifiers modifiers, out bool ctrlOrCmd, out bool shift)
        {
            ctrlOrCmd = modifiers.HasFlag(GetPlatformCtrlOrCmdKeyModifier(target));
            shift = modifiers.HasFlag(KeyModifiers.Shift);
        }

        public static void GetMetaKeyState(Control target, KeyModifiers modifiers, out bool ctrlOrCmd, out bool shift, out bool alt)
        {
            ctrlOrCmd = modifiers.HasFlag(GetPlatformCtrlOrCmdKeyModifier(target));
            shift = modifiers.HasFlag(KeyModifiers.Shift);
            alt = modifiers.HasFlag(KeyModifiers.Alt);
        }

        public static KeyModifiers GetPlatformCtrlOrCmdKeyModifier(Control target)
        {
            var keymap = TopLevel.GetTopLevel(target)!.PlatformSettings!.HotkeyConfiguration;
            return keymap.CommandModifiers;
        }
    }
}
