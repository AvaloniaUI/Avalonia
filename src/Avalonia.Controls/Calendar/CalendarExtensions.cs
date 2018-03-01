// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using Avalonia.Input;
using System.Diagnostics;

namespace Avalonia.Controls.Primitives
{
    internal static class CalendarExtensions
    {
        public static void GetMetaKeyState(InputModifiers modifiers, out bool ctrl, out bool shift)
        {
            ctrl = (modifiers & InputModifiers.Control) == InputModifiers.Control;
            shift = (modifiers & InputModifiers.Shift) == InputModifiers.Shift;
        }
    }
}
