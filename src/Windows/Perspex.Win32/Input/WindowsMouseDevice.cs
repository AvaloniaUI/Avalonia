// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Input;
using Perspex.Interactivity;
using Perspex.Win32.Interop;

namespace Perspex.Win32.Input
{
    public class WindowsMouseDevice : MouseDevice
    {
        private static readonly WindowsMouseDevice s_instance = new WindowsMouseDevice();

        public static new WindowsMouseDevice Instance => s_instance;

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