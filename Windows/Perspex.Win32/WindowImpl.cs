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
    using System.Runtime.InteropServices;
    using Perspex.Controls;
    using Perspex.Input.Raw;
    using Perspex.Platform;
    using Perspex.Win32.Input;
    using Perspex.Win32.Interop;

    public class WindowImpl : IWindowImpl
    {
        private UnmanagedMethods.WndProc wndProcDelegate;

        private string className;

        private IntPtr hwnd;

        private TopLevel owner;

        private bool trackingMouse;

        public WindowImpl()
        {
            this.CreateWindow();
        }

        public Action Activated { get; set; }

        public Action Closed { get; set; }

        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect, IPlatformHandle> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public Size ClientSize
        {
            get
            {
                UnmanagedMethods.RECT rect;
                UnmanagedMethods.GetClientRect(this.hwnd, out rect);
                return new Size(rect.right, rect.bottom);
            }

            set
            {
                if (value != this.ClientSize)
                {
                    var style = UnmanagedMethods.GetWindowLong(this.hwnd, -16);
                    var exStyle = UnmanagedMethods.GetWindowLong(this.hwnd, -20);
                    var padding = new UnmanagedMethods.RECT();

                    if (UnmanagedMethods.AdjustWindowRectEx(ref padding, style, false, exStyle))
                    {
                        UnmanagedMethods.SetWindowPos(
                            this.hwnd,
                            IntPtr.Zero,
                            0,
                            0,
                            -padding.left + padding.right + (int)value.Width,
                            -padding.top + padding.bottom + (int)value.Height,
                            UnmanagedMethods.SetWindowPosFlags.SWP_RESIZE);
                    }
                }
            }
        }

        public IPlatformHandle Handle
        {
            get;
            private set;
        }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl();
        }

        public void Dispose()
        {
            UnmanagedMethods.DestroyWindow(this.hwnd);
        }

        public void Hide()
        {
            UnmanagedMethods.ShowWindow(this.hwnd, UnmanagedMethods.ShowWindowCommand.Hide);
        }

        public void Invalidate(Rect rect)
        {
            var r = new UnmanagedMethods.RECT
            {
                left = (int)rect.X,
                top = (int)rect.Y,
                right = (int)rect.Right,
                bottom = (int)rect.Bottom,
            };

            UnmanagedMethods.InvalidateRect(this.hwnd, ref r, false);
        }

        public Point PointToScreen(Point point)
        {
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ClientToScreen(this.hwnd, ref p);
            return new Point(p.X, p.Y);
        }

        public void SetOwner(TopLevel owner)
        {
            this.owner = owner;
        }

        public void SetTitle(string title)
        {
            UnmanagedMethods.SetWindowText(this.hwnd, title);
        }

        public virtual void Show()
        {
            UnmanagedMethods.ShowWindow(this.hwnd, UnmanagedMethods.ShowWindowCommand.Normal);
        }

        protected virtual IntPtr CreateWindowOverride(ushort atom)
        {
            return UnmanagedMethods.CreateWindowEx(
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
        }

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
        protected virtual IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const double WheelDelta = 120.0;
            uint timestamp = unchecked((uint)UnmanagedMethods.GetMessageTime());

            RawInputEventArgs e = null;

            WindowsMouseDevice.Instance.CurrentWindow = this;

            switch ((UnmanagedMethods.WindowsMessage)msg)
            {
                case UnmanagedMethods.WindowsMessage.WM_ACTIVATE:
                    switch ((int)lParam & 0xffff)
                    {
                        case 1:
                        case 2:
                            if (this.Activated != null)
                            {
                                this.Activated();
                            }

                            break;
                        case 0:
                            if (this.Deactivated != null)
                            {
                                this.Deactivated();
                            }

                            break;
                    }

                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_DESTROY:
                    if (this.Closed != null)
                    {
                        this.Closed();
                    }

                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_KEYDOWN:
                    WindowsKeyboardDevice.Instance.UpdateKeyStates();
                    e = new RawKeyEventArgs(
                            WindowsKeyboardDevice.Instance,
                            timestamp,
                            RawKeyEventType.KeyDown,
                            KeyInterop.KeyFromVirtualKey((int)wParam),
                            WindowsKeyboardDevice.Instance.StringFromVirtualKey((uint)wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_LBUTTONDOWN:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        this.owner,
                        RawMouseEventType.LeftButtonDown,
                        new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_LBUTTONUP:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        this.owner,
                        RawMouseEventType.LeftButtonUp,
                        new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSEMOVE:
                    if (!this.trackingMouse)
                    {
                        var tm = new UnmanagedMethods.TRACKMOUSEEVENT
                        {
                            cbSize = Marshal.SizeOf(typeof(UnmanagedMethods.TRACKMOUSEEVENT)),
                            dwFlags = 2,
                            hwndTrack = this.hwnd,
                            dwHoverTime = 0,
                        };

                        UnmanagedMethods.TrackMouseEvent(ref tm);
                    }

                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        this.owner,
                        RawMouseEventType.Move,
                        new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSEWHEEL:
                    e = new RawMouseWheelEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        this.owner,
                        this.ScreenToClient((uint)lParam & 0xffff, (uint)lParam >> 16),
                        new Vector(0, ((int)wParam >> 16) / WheelDelta));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSELEAVE:
                    this.trackingMouse = false;
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        this.owner,
                        RawMouseEventType.LeaveWindow,
                        new Point());
                    break;

                case UnmanagedMethods.WindowsMessage.WM_PAINT:
                    if (this.Paint != null)
                    {
                        UnmanagedMethods.PAINTSTRUCT ps;

                        if (UnmanagedMethods.BeginPaint(this.hwnd, out ps) != IntPtr.Zero)
                        {
                            UnmanagedMethods.RECT r;
                            UnmanagedMethods.GetUpdateRect(this.hwnd, out r, false);
                            this.Paint(new Rect(r.left, r.top, r.right - r.left, r.bottom - r.top), this.Handle);
                            UnmanagedMethods.EndPaint(this.hwnd, ref ps);
                        }
                    }
                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_SIZE:
                    if (this.Resized != null)
                    {
                        var clientSize = new Size((int)lParam & 0xffff, (int)lParam >> 16);
                        this.Resized(clientSize);
                    }

                    return IntPtr.Zero;
            }

            if (e != null && this.Input != null)
            {
                this.Input(e);
                return IntPtr.Zero;
            }

            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
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

            ushort atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            this.hwnd = this.CreateWindowOverride(atom);

            if (this.hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            this.Handle = new PlatformHandle(this.hwnd, "HWND");
        }

        private Point ScreenToClient(uint x, uint y)
        {
            var p = new UnmanagedMethods.POINT { X = (int)x, Y = (int)y };
            UnmanagedMethods.ScreenToClient(this.hwnd, ref p);
            return new Point(p.X, p.Y);
        }
    }
}
