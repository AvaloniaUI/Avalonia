// -----------------------------------------------------------------------
// <copyright file="WindowImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Win32
{
    using System;
    using Perspex.Platform;
    using Perspex.Win32.Interop;

    public class PopupImpl : WindowImpl, IPopupImpl
    {
        public void SetPosition(Point p)
        {
            UnmanagedMethods.SetWindowPos(
                this.Handle.Handle, 
                IntPtr.Zero, 
                (int)p.X, 
                (int)p.Y, 
                0,
                0,
                UnmanagedMethods.SetWindowPosFlags.SWP_NOSIZE);
        }

        protected override IntPtr CreateWindowOverride(ushort atom)
        {
            UnmanagedMethods.WindowStyles style =
                UnmanagedMethods.WindowStyles.WS_POPUP |
                UnmanagedMethods.WindowStyles.WS_CLIPSIBLINGS;

            UnmanagedMethods.WindowStyles exStyle =
                UnmanagedMethods.WindowStyles.WS_EX_TOOLWINDOW |
                UnmanagedMethods.WindowStyles.WS_EX_TOPMOST |
                UnmanagedMethods.WindowStyles.WS_EX_NOACTIVATE;

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
    }
}
