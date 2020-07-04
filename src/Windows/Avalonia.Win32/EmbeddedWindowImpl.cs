using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class EmbeddedWindowImpl : WindowImpl
    {
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
                OffscreenParentWindow.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            return hWnd;
        }

    }
}
