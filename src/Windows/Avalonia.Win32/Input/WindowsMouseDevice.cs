// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Input
{
    class WindowsMouseDevice : MouseDevice
    {
        public static WindowsMouseDevice Instance { get; } = new WindowsMouseDevice();

        public WindowsMouseDevice() : base(new WindowsMousePointer())
        {
            
        }
        
        public WindowImpl CurrentWindow
        {
            get;
            set;
        }

        class WindowsMousePointer : Pointer
        {
            public WindowsMousePointer() : base(Pointer.GetNextFreeId(),PointerType.Mouse, true)
            {
            }

            protected override void PlatformCapture(IInputElement element)
            {
                var hwnd = ((element?.GetVisualRoot() as TopLevel)?.PlatformImpl as WindowImpl)
                    ?.Handle.Handle;

                if (hwnd.HasValue && hwnd != IntPtr.Zero)
                    UnmanagedMethods.SetCapture(hwnd.Value);
                else
                    UnmanagedMethods.ReleaseCapture();
            }
        }
    }
}
