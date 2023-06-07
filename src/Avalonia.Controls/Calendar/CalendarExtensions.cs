// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Input;

namespace Avalonia.Controls.Primitives
{
    internal static class CalendarExtensions
    {
        public static void GetMetaKeyState(KeyModifiers modifiers, out bool ctrl, out bool shift)
        {
            ctrl = (modifiers & KeyModifiers.Control) == KeyModifiers.Control;
            shift = (modifiers & KeyModifiers.Shift) == KeyModifiers.Shift;
        }
    }
}
