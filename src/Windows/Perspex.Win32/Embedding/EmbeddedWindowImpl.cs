// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Win32.Interop;

namespace Perspex.Win32
{
    public class EmbeddedWindowImpl : WindowImpl
    {
        private static readonly System.Windows.Forms.UserControl s_winFormsControl = new System.Windows.Forms.UserControl();

        public IntPtr Handle { get; private set; }

        protected override IntPtr CreateWindowOverride(ushort atom)
        {
            var hWnd = UnmanagedMethods.CreateWindowEx(
                0,
                atom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_CHILD,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                s_winFormsControl.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            this.Handle = hWnd;
            return hWnd;
        }
    }
}
