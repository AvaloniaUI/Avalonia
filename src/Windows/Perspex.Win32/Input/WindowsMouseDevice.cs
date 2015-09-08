





namespace Perspex.Win32.Input
{
    using System;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Perspex.Win32.Interop;

    public class WindowsMouseDevice : MouseDevice
    {
        private static WindowsMouseDevice instance = new WindowsMouseDevice();

        public static new WindowsMouseDevice Instance
        {
            get { return instance; }
        }

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
                UnmanagedMethods.SetCapture(this.CurrentWindow.Handle.Handle);
            }
            else
            {
                UnmanagedMethods.ReleaseCapture();
            }
        }
    }
}