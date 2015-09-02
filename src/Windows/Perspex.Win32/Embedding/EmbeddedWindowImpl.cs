namespace Perspex.Win32
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Interop;
    using Perspex.Win32.Interop;

    public class EmbeddedWindowImpl : WindowImpl
    {
        private static readonly System.Windows.Forms.UserControl WinFormsControl = new System.Windows.Forms.UserControl();

        public HwndHost Host { get; set; }

        private class FakeHost : HwndHost
        {
            private readonly IntPtr hWnd;

            public FakeHost(IntPtr hWnd)
            {
                this.hWnd = hWnd;
            }

            protected override HandleRef BuildWindowCore(HandleRef hwndParent)
            {
                UnmanagedMethods.SetParent(this.hWnd, hwndParent.Handle);
                return new HandleRef(this, this.hWnd);
            }

            protected override void DestroyWindowCore(HandleRef hwnd)
            {
            }
        }

        public IntPtr Handle { get; private set; }

        public HwndHost CreateWpfHost()
        {
            return new FakeHost(this.Handle);
        }

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
