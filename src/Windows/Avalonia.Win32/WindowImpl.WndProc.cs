// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Win32.Input;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    public partial class WindowImpl
    {
        protected virtual unsafe IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr lRet = IntPtr.Zero;
            bool callDwp = true;

            if (_isClientAreaExtended)
            {
                lRet = CustomCaptionProc(hWnd, msg, wParam, lParam, ref callDwp);
            }

            if (callDwp)
            {
                lRet = AppWndProc(hWnd, msg, wParam, lParam);
            }

            return lRet;
        }
    }
}
