// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    public class EmbeddedWindowImpl : WindowImpl
    {
#if NOT_NETSTANDARD
        private static readonly System.Windows.Forms.UserControl WinFormsControl = new System.Windows.Forms.UserControl();
#endif
        protected override IntPtr CreateWindowOverride(ushort atom)
        {
#if !NOT_NETSTANDARD
            throw new NotImplementedException();
#else
            var hWnd = UnmanagedMethods.CreateWindowEx(
                0,
                atom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_CHILD,
                0,
                0,
                640,
                480,
                WinFormsControl.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            return hWnd;
#endif
        }
    }
}
