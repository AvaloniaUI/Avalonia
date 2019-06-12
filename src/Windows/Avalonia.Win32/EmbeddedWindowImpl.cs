// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class EmbeddedWindowImpl : WindowImpl, IEmbeddableWindowImpl
    {
        private static IntPtr DefaultParentWindow = CreateParentWindow();
        private static UnmanagedMethods.WndProc _wndProcDelegate;

        protected override IntPtr CreateWindowOverride(ushort atom)
        {
            var hWnd = UnmanagedMethods.CreateWindowEx(
                0,
                atom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_CHILD,
                0,
                0,
                640,
                480,
                DefaultParentWindow,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            return hWnd;
        }

        protected override IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (uint)UnmanagedMethods.WindowsMessage.WM_KILLFOCUS)
                LostFocus?.Invoke();
            return base.WndProc(hWnd, msg, wParam, lParam);
        }

        public event Action LostFocus;

        private static IntPtr CreateParentWindow()
        {
            _wndProcDelegate = new UnmanagedMethods.WndProc(ParentWndProc);

            var wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
                hInstance = UnmanagedMethods.GetModuleHandle(null),
                lpfnWndProc = _wndProcDelegate,
                lpszClassName = "AvaloniaEmbeddedWindow-" + Guid.NewGuid(),
            };

            var atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            var hwnd = UnmanagedMethods.CreateWindowEx(
                0,
                atom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_OVERLAPPEDWINDOW,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return hwnd;
        }

        private static IntPtr ParentWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
