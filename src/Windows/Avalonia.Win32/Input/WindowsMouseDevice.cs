// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Input
{
    class WindowsMouseDevice : MouseDevice
    {
        public static WindowsMouseDevice Instance { get; } = new WindowsMouseDevice();

        public WindowImpl CurrentWindow
        {
            get;
            set;
        }

        public override void Capture(IInputElement control)
        {
            base.Capture(control);

            if (control != null)
            {
                UnmanagedMethods.SetCapture(CurrentWindow.Handle.Handle);
            }
            else
            {
                UnmanagedMethods.ReleaseCapture();
            }
        }
    }
}