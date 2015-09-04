namespace Perspex.Win32
{
    using System;
    using Perspex.Win32.Interop;

    public class EmbeddedWindowImpl : WindowImpl
    {
        private static readonly System.Windows.Forms.UserControl WinFormsControl = new System.Windows.Forms.UserControl();

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
                WinFormsControl.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            this.Handle = hWnd;
            return hWnd;
        }
    }
}
