// -----------------------------------------------------------------------
// <copyright file="WindowImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Win32
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Reactive.Linq;
    using System.Runtime.InteropServices;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Diagnostics;
    using Perspex.Input;
    using Perspex.Input.Raw;
    using Perspex.Layout;
    using Perspex.Platform;
    using Perspex.Rendering;
    using Perspex.Threading;
    using Perspex.Win32.Input;
    using Perspex.Win32.Interop;
    using Splat;

    public class WindowImpl : IWindowImpl
    {
        private UnmanagedMethods.WndProc wndProcDelegate;

        private string className;

        private IntPtr hwnd;

        private Window owner;

        public WindowImpl()
        {
            this.CreateWindow();
        }

        public event EventHandler Activated;

        public event EventHandler Closed;

        public event EventHandler<RawInputEventArgs> Input;

        public event EventHandler<RawSizeEventArgs> Resized;

        public Size ClientSize
        {
            get
            {
                UnmanagedMethods.RECT rect;
                UnmanagedMethods.GetClientRect(this.hwnd, out rect);
                return new Size(rect.right, rect.bottom);
            }
        }

        public IPlatformHandle Handle
        {
            get;
            private set;
        }

        public void SetOwner(Window owner)
        {
            this.owner = owner;
        }

        public void SetTitle(string title)
        {
            UnmanagedMethods.SetWindowText(this.hwnd, title);
        }

        public void Show()
        {
            UnmanagedMethods.ShowWindow(this.hwnd, 1);
        }

        private void CreateWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            this.wndProcDelegate = new UnmanagedMethods.WndProc(this.WndProc);

            this.className = Guid.NewGuid().ToString();

            UnmanagedMethods.WNDCLASSEX wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(UnmanagedMethods.WNDCLASSEX)),
                style = 0,
                lpfnWndProc = this.wndProcDelegate,
                hInstance = Marshal.GetHINSTANCE(this.GetType().Module),
                hCursor = UnmanagedMethods.LoadCursor(IntPtr.Zero, (int)UnmanagedMethods.Cursor.IDC_ARROW),
                hbrBackground = (IntPtr)5,
                lpszClassName = this.className,
            };

            System.Diagnostics.Debug.WriteLine("Registered class " + this.className);

            ushort atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            this.hwnd = UnmanagedMethods.CreateWindowEx(
                0,
                atom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_OVERLAPPEDWINDOW,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (this.hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            this.Handle = new PlatformHandle(this.hwnd, "HWND");
        }

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            RawInputEventArgs e = null;

            WindowsMouseDevice.Instance.CurrentWindow = this;

            switch ((UnmanagedMethods.WindowsMessage)msg)
            {
                case UnmanagedMethods.WindowsMessage.WM_ACTIVATE:
                    if (this.Activated != null)
                    {
                        this.Activated(this, EventArgs.Empty);
                    }
                    break;

                case UnmanagedMethods.WindowsMessage.WM_DESTROY:
                    if (this.Closed != null)
                    {
                        this.Closed(this, EventArgs.Empty);
                    }
                    break;

                case UnmanagedMethods.WindowsMessage.WM_KEYDOWN:
                    WindowsKeyboardDevice.Instance.UpdateKeyStates();
                    e = new RawKeyEventArgs(
                            WindowsKeyboardDevice.Instance,
                            RawKeyEventType.KeyDown,
                            KeyInterop.KeyFromVirtualKey((int)wParam),
                            WindowsKeyboardDevice.Instance.StringFromVirtualKey((uint)wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_LBUTTONDOWN:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        this.owner,
                        RawMouseEventType.LeftButtonDown,
                        new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_LBUTTONUP:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        this.owner,
                        RawMouseEventType.LeftButtonUp,
                        new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSEMOVE:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        this.owner,
                        RawMouseEventType.Move,
                        new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_SIZE:
                    if (this.Resized != null)
                    {
                        this.Resized(this, new RawSizeEventArgs((int)lParam & 0xffff, (int)lParam >> 16));
                    }
                    return IntPtr.Zero;
            }

            if (e != null && this.Input != null)
            {
                this.Input(this, e);
            }

            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
