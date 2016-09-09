// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class PopupImpl : WindowImpl, IPopupImpl
    {
        public override void Show()
        {
            UnmanagedMethods.ShowWindow(Handle.Handle, UnmanagedMethods.ShowWindowCommand.ShowNoActivate);
        }

        protected override IntPtr CreateWindowOverride(ushort atom)
        {
            UnmanagedMethods.WindowStyles style =
                UnmanagedMethods.WindowStyles.WS_POPUP |
                UnmanagedMethods.WindowStyles.WS_CLIPSIBLINGS;

            UnmanagedMethods.WindowStyles exStyle =
                UnmanagedMethods.WindowStyles.WS_EX_TOOLWINDOW |
                UnmanagedMethods.WindowStyles.WS_EX_TOPMOST;

            return UnmanagedMethods.CreateWindowEx(
                (int)exStyle,
                atom,
                null,
                (uint)style,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        protected override IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch ((UnmanagedMethods.WindowsMessage)msg)
            {
                case UnmanagedMethods.WindowsMessage.WM_MOUSEACTIVATE:
                    return (IntPtr)UnmanagedMethods.MouseActivate.MA_NOACTIVATE;
                default:
                    return base.WndProc(hWnd, msg, wParam, lParam);
            }
        }
    }
}
