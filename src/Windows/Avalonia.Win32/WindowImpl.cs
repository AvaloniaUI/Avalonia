// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    public class WindowImpl : IWindowImpl
    {
        private static readonly List<WindowImpl> s_instances = new List<WindowImpl>();

        private static readonly IntPtr DefaultCursor = UnmanagedMethods.LoadCursor(
            IntPtr.Zero, new IntPtr((int)UnmanagedMethods.Cursor.IDC_ARROW));

        private UnmanagedMethods.WndProc _wndProcDelegate;
        private string _className;
        private IntPtr _hwnd;
        private IInputRoot _owner;
        private bool _trackingMouse;
        private bool _isActive;
        private bool _decorated = true;
        private double _scaling = 1;
        private WindowState _showWindowState;

        public WindowImpl()
        {
            CreateWindow();
            s_instances.Add(this);
        }

        public Action Activated { get; set; }

        public Action Closed { get; set; }

        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public Thickness BorderThickness
        {
            get
            {
                var style = UnmanagedMethods.GetWindowLong(_hwnd, -16);
                var exStyle = UnmanagedMethods.GetWindowLong(_hwnd, -20);
                var padding = new UnmanagedMethods.RECT();

                if (UnmanagedMethods.AdjustWindowRectEx(ref padding, style, false, exStyle))
                {
                    return new Thickness(-padding.left, -padding.top, padding.right, padding.bottom);
                }
                else
                {
                    throw new Win32Exception();
                }
            }
        }

        public Size ClientSize
        {
            get
            {
                UnmanagedMethods.RECT rect;
                UnmanagedMethods.GetClientRect(_hwnd, out rect);
                return new Size(rect.right, rect.bottom) / Scaling;
            }

            set
            {
                if (value != ClientSize)
                {
                    value *= Scaling;
                    value += BorderThickness;

                    UnmanagedMethods.SetWindowPos(
                        _hwnd,
                        IntPtr.Zero,
                        0,
                        0,
                        (int)value.Width,
                        (int)value.Height,
                        UnmanagedMethods.SetWindowPosFlags.SWP_RESIZE);
                }
            }
        }

        public double Scaling => _scaling;

        public IPlatformHandle Handle
        {
            get;
            private set;
        }

        public bool IsEnabled
        {
            get { return UnmanagedMethods.IsWindowEnabled(_hwnd); }
            set { UnmanagedMethods.EnableWindow(_hwnd, value); }
        }

        public Size MaxClientSize
        {
            get
            {
                return (new Size(
                    UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CXMAXTRACK),
                    UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CYMAXTRACK))
                    - BorderThickness) / Scaling;
            }
        }

        public WindowState WindowState
        {
            get
            {
                var placement = default(UnmanagedMethods.WINDOWPLACEMENT);
                UnmanagedMethods.GetWindowPlacement(_hwnd, ref placement);
                
                switch (placement.ShowCmd)
                {
                    case UnmanagedMethods.ShowWindowCommand.Maximize:
                        return WindowState.Maximized;
                    case UnmanagedMethods.ShowWindowCommand.Minimize:
                        return WindowState.Minimized;
                    default:
                        return WindowState.Normal;
                }
            }

            set
            {
                if (UnmanagedMethods.IsWindowVisible(_hwnd))
                {
                    ShowWindow(value);
                }
                else
                {
                    _showWindowState = value;
                }
            }
        }

        public void Activate()
        {
            UnmanagedMethods.SetActiveWindow(_hwnd);
        }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl();
        }

        public void Dispose()
        {
            s_instances.Remove(this);
            UnmanagedMethods.DestroyWindow(_hwnd);
        }

        public void Hide()
        {
            UnmanagedMethods.ShowWindow(_hwnd, UnmanagedMethods.ShowWindowCommand.Hide);
        }

        public void SetSystemDecorations(bool value)
        {
            if (value == _decorated)
                return;
            var style = (UnmanagedMethods.WindowStyles) UnmanagedMethods.GetWindowLong(_hwnd, -16);
            style |= UnmanagedMethods.WindowStyles.WS_OVERLAPPEDWINDOW;
            if (!value)
                style ^= UnmanagedMethods.WindowStyles.WS_OVERLAPPEDWINDOW;

            UnmanagedMethods.RECT windowRect;

            UnmanagedMethods.GetWindowRect(_hwnd, out windowRect);
            Rect newRect;
            var oldThickness = BorderThickness;

            UnmanagedMethods.SetWindowLong(_hwnd, -16, (uint) style);
            if (value)
            {
                var thickness = BorderThickness;
                newRect = new Rect(
                    windowRect.left - thickness.Left,
                    windowRect.top - thickness.Top,
                    (windowRect.right - windowRect.left) + (thickness.Left + thickness.Right),
                    (windowRect.bottom - windowRect.top) + (thickness.Top + thickness.Bottom));
            }
            else
                newRect = new Rect(
                    windowRect.left + oldThickness.Left,
                    windowRect.top + oldThickness.Top,
                    (windowRect.right - windowRect.left) - (oldThickness.Left + oldThickness.Right),
                    (windowRect.bottom - windowRect.top) - (oldThickness.Top + oldThickness.Bottom));
            UnmanagedMethods.SetWindowPos(_hwnd, IntPtr.Zero, (int) newRect.X, (int) newRect.Y, (int) newRect.Width,
                (int) newRect.Height,
                UnmanagedMethods.SetWindowPosFlags.SWP_NOZORDER | UnmanagedMethods.SetWindowPosFlags.SWP_NOACTIVATE);


            _decorated = value;


        }

        public void Invalidate(Rect rect)
        {
            var f = Scaling;
            var r = new UnmanagedMethods.RECT
            {
                left = (int)(rect.X * f),
                top = (int)(rect.Y * f),
                right = (int)(rect.Right * f),
                bottom = (int)(rect.Bottom * f),
            };

            UnmanagedMethods.InvalidateRect(_hwnd, ref r, false);
        }

        public Point PointToClient(Point point)
        {
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y) / Scaling;
        }

        public Point PointToScreen(Point point)
        {
            point *= Scaling;
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ClientToScreen(_hwnd, ref p);
            return new Point(p.X, p.Y);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _owner = inputRoot;
        }

        public void SetTitle(string title)
        {
            UnmanagedMethods.SetWindowText(_hwnd, title);
        }

        public virtual void Show()
        {
            ShowWindow(_showWindowState);
        }

        public void BeginMoveDrag()
        {
            UnmanagedMethods.DefWindowProc(_hwnd, (int) UnmanagedMethods.WindowsMessage.WM_NCLBUTTONDOWN,
                new IntPtr((int)UnmanagedMethods.HitTestValues.HTCAPTION), IntPtr.Zero);
        }

        static readonly Dictionary<WindowEdge, UnmanagedMethods.HitTestValues> EdgeDic = new Dictionary<WindowEdge, UnmanagedMethods.HitTestValues>
        {
            {WindowEdge.East, UnmanagedMethods.HitTestValues.HTRIGHT},
            {WindowEdge.North, UnmanagedMethods.HitTestValues.HTTOP },
            {WindowEdge.NorthEast, UnmanagedMethods.HitTestValues.HTTOPRIGHT },
            {WindowEdge.NorthWest, UnmanagedMethods.HitTestValues.HTTOPLEFT },
            {WindowEdge.South, UnmanagedMethods.HitTestValues.HTBOTTOM },
            {WindowEdge.SouthEast, UnmanagedMethods.HitTestValues.HTBOTTOMRIGHT },
            {WindowEdge.SouthWest, UnmanagedMethods.HitTestValues.HTBOTTOMLEFT },
            {WindowEdge.West, UnmanagedMethods.HitTestValues.HTLEFT}
        };

        public void BeginResizeDrag(WindowEdge edge)
        {
            UnmanagedMethods.DefWindowProc(_hwnd, (int) UnmanagedMethods.WindowsMessage.WM_NCLBUTTONDOWN,
                new IntPtr((int) EdgeDic[edge]), IntPtr.Zero);
        }

        public Point Position
        {
            get
            {
                UnmanagedMethods.RECT rc;
                UnmanagedMethods.GetWindowRect(_hwnd, out rc);
                return new Point(rc.left, rc.top);
            }
            set
            {
                UnmanagedMethods.SetWindowPos(
                    Handle.Handle,
                    IntPtr.Zero,
                    (int) value.X,
                    (int) value.Y,
                    0,
                    0,
                    UnmanagedMethods.SetWindowPosFlags.SWP_NOSIZE | UnmanagedMethods.SetWindowPosFlags.SWP_NOACTIVATE);

            }
        }

        public virtual IDisposable ShowDialog()
        {
            var disabled = s_instances.Where(x => x != this && x.IsEnabled).ToList();
            WindowImpl activated = null;

            foreach (var window in disabled)
            {
                if (window._isActive)
                {
                    activated = window;
                }

                window.IsEnabled = false;
            }

            Show();

            return Disposable.Create(() =>
            {
                foreach (var window in disabled)
                {
                    window.IsEnabled = true;
                }

                activated?.Activate();
            });
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            UnmanagedMethods.SetClassLong(_hwnd, UnmanagedMethods.ClassLongIndex.GCL_HCURSOR,
                cursor?.Handle ?? DefaultCursor);
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
            bool unicode = UnmanagedMethods.IsWindowUnicode(hWnd);

            const double wheelDelta = 120.0;
            uint timestamp = unchecked((uint)UnmanagedMethods.GetMessageTime());

            RawInputEventArgs e = null;

            WindowsMouseDevice.Instance.CurrentWindow = this;

            switch ((UnmanagedMethods.WindowsMessage)msg)
            {
                case UnmanagedMethods.WindowsMessage.WM_ACTIVATE:
                    var wa = (UnmanagedMethods.WindowActivate)((int)wParam & 0xffff);

                    switch (wa)
                    {
                        case UnmanagedMethods.WindowActivate.WA_ACTIVE:
                        case UnmanagedMethods.WindowActivate.WA_CLICKACTIVE:
                            _isActive = true;
                            Activated?.Invoke();
                            break;

                        case UnmanagedMethods.WindowActivate.WA_INACTIVE:
                            _isActive = false;
                            Deactivated?.Invoke();
                            break;
                    }

                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_DESTROY:
                    if (Closed != null)
                    {
                        UnmanagedMethods.UnregisterClass(_className, Marshal.GetHINSTANCE(GetType().Module));
                        Closed();
                    }

                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_DPICHANGED:
                    var dpi = (int)wParam & 0xffff;
                    var newDisplayRect = (UnmanagedMethods.RECT)Marshal.PtrToStructure(lParam, typeof(UnmanagedMethods.RECT));
                    Position = new Point(newDisplayRect.left, newDisplayRect.top);
                    _scaling = dpi / 96.0;
                    ScalingChanged?.Invoke(_scaling);
                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_KEYDOWN:
                case UnmanagedMethods.WindowsMessage.WM_SYSKEYDOWN:
                    e = new RawKeyEventArgs(
                            WindowsKeyboardDevice.Instance,
                            timestamp,
                            RawKeyEventType.KeyDown,
                            KeyInterop.KeyFromVirtualKey((int)wParam), WindowsKeyboardDevice.Instance.Modifiers);
                    break;

                case UnmanagedMethods.WindowsMessage.WM_KEYUP:
                case UnmanagedMethods.WindowsMessage.WM_SYSKEYUP:
                    e = new RawKeyEventArgs(
                            WindowsKeyboardDevice.Instance,
                            timestamp,
                            RawKeyEventType.KeyUp,
                            KeyInterop.KeyFromVirtualKey((int)wParam), WindowsKeyboardDevice.Instance.Modifiers);
                    break;
                case UnmanagedMethods.WindowsMessage.WM_CHAR:
                    // Ignore control chars
                    if (wParam.ToInt32() >= 32)
                    {
                        e = new RawTextInputEventArgs(WindowsKeyboardDevice.Instance, timestamp,
                            new string((char)wParam.ToInt32(), 1));
                    }

                    break;

                case UnmanagedMethods.WindowsMessage.WM_LBUTTONDOWN:
                case UnmanagedMethods.WindowsMessage.WM_RBUTTONDOWN:
                case UnmanagedMethods.WindowsMessage.WM_MBUTTONDOWN:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        _owner,
                        msg == (int)UnmanagedMethods.WindowsMessage.WM_LBUTTONDOWN
                            ? RawMouseEventType.LeftButtonDown
                            : msg == (int)UnmanagedMethods.WindowsMessage.WM_RBUTTONDOWN
                                ? RawMouseEventType.RightButtonDown
                                : RawMouseEventType.MiddleButtonDown,
                        DipFromLParam(lParam), GetMouseModifiers(wParam));
                    break;
                    
                case UnmanagedMethods.WindowsMessage.WM_LBUTTONUP:
                case UnmanagedMethods.WindowsMessage.WM_RBUTTONUP:
                case UnmanagedMethods.WindowsMessage.WM_MBUTTONUP:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        _owner,
                        msg == (int) UnmanagedMethods.WindowsMessage.WM_LBUTTONUP
                            ? RawMouseEventType.LeftButtonUp
                            : msg == (int) UnmanagedMethods.WindowsMessage.WM_RBUTTONUP
                                ? RawMouseEventType.RightButtonUp
                                : RawMouseEventType.MiddleButtonUp,
                        DipFromLParam(lParam), GetMouseModifiers(wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSEMOVE:
                    if (!_trackingMouse)
                    {
                        var tm = new UnmanagedMethods.TRACKMOUSEEVENT
                        {
                            cbSize = Marshal.SizeOf(typeof(UnmanagedMethods.TRACKMOUSEEVENT)),
                            dwFlags = 2,
                            hwndTrack = _hwnd,
                            dwHoverTime = 0,
                        };

                        UnmanagedMethods.TrackMouseEvent(ref tm);
                    }

                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        _owner,
                        RawMouseEventType.Move,
                        DipFromLParam(lParam), GetMouseModifiers(wParam));

                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSEWHEEL:
                    e = new RawMouseWheelEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        _owner,
                        ScreenToClient(DipFromLParam(lParam)),
                        new Vector(0, ((int)wParam >> 16) / wheelDelta), GetMouseModifiers(wParam));
                    break;
                case UnmanagedMethods.WindowsMessage.WM_MOUSEHWHEEL:
                    e = new RawMouseWheelEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        _owner,
                        ScreenToClient(DipFromLParam(lParam)),
                        new Vector(-((int)wParam >> 16) / wheelDelta,0), GetMouseModifiers(wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSELEAVE:
                    _trackingMouse = false;
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        _owner,
                        RawMouseEventType.LeaveWindow,
                        new Point(), WindowsKeyboardDevice.Instance.Modifiers);
                    break;

                case UnmanagedMethods.WindowsMessage.WM_NCLBUTTONDOWN:
                case UnmanagedMethods.WindowsMessage.WM_NCRBUTTONDOWN:
                case UnmanagedMethods.WindowsMessage.WM_NCMBUTTONDOWN:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance,
                        timestamp,
                        _owner,
                        msg == (int)UnmanagedMethods.WindowsMessage.WM_NCLBUTTONDOWN
                            ? RawMouseEventType.NonClientLeftButtonDown
                            : msg == (int)UnmanagedMethods.WindowsMessage.WM_NCRBUTTONDOWN
                                ? RawMouseEventType.RightButtonDown
                                : RawMouseEventType.MiddleButtonDown,
                        new Point(0, 0), GetMouseModifiers(wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_PAINT:
                    if (Paint != null)
                    {
                        UnmanagedMethods.PAINTSTRUCT ps;

                        if (UnmanagedMethods.BeginPaint(_hwnd, out ps) != IntPtr.Zero)
                        {
                            UnmanagedMethods.RECT r;
                            UnmanagedMethods.GetUpdateRect(_hwnd, out r, false);
                            var f = Scaling;
                            Paint(new Rect(r.left / f, r.top / f, (r.right - r.left) / f, (r.bottom - r.top) / f));
                            UnmanagedMethods.EndPaint(_hwnd, ref ps);
                        }
                    }

                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_SIZE:
                    if (Resized != null)
                    {
                        var clientSize = new Size((int)lParam & 0xffff, (int)lParam >> 16);
                        Resized(clientSize / Scaling);
                    }

                    return IntPtr.Zero;
            }

            if (e != null && Input != null)
            {
                Input(e);

                if (e.Handled)
                {
                    return IntPtr.Zero;
                }
            }

            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        static InputModifiers GetMouseModifiers(IntPtr wParam)
        {
            var keys = (UnmanagedMethods.ModifierKeys)wParam.ToInt32();
            var modifiers = WindowsKeyboardDevice.Instance.Modifiers;
            if (keys.HasFlag(UnmanagedMethods.ModifierKeys.MK_LBUTTON))
                modifiers |= InputModifiers.LeftMouseButton;
            if(keys.HasFlag(UnmanagedMethods.ModifierKeys.MK_RBUTTON))
                modifiers  |= InputModifiers.RightMouseButton;
            if(keys.HasFlag(UnmanagedMethods.ModifierKeys.MK_MBUTTON))
                modifiers |= InputModifiers.MiddleMouseButton;
            return modifiers;
        }

        private void CreateWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = new UnmanagedMethods.WndProc(WndProc);

            _className = Guid.NewGuid().ToString();

            UnmanagedMethods.WNDCLASSEX wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(UnmanagedMethods.WNDCLASSEX)),
                style = 0,
                lpfnWndProc = _wndProcDelegate,
                hInstance = Marshal.GetHINSTANCE(GetType().Module),
                hCursor = DefaultCursor,
                hbrBackground = IntPtr.Zero,
                lpszClassName = _className,
            };

            ushort atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            _hwnd = CreateWindowOverride(atom);

            if (_hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            Handle = new PlatformHandle(_hwnd, PlatformConstants.WindowHandleType);

            if (UnmanagedMethods.ShCoreAvailable)
            {
                uint dpix, dpiy;

                var monitor = UnmanagedMethods.MonitorFromWindow(
                    _hwnd,
                    UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                        monitor,
                        UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out dpix,
                        out dpiy) == 0)
                {
                    _scaling = dpix / 96.0;
                }
            }
        }

        private Point DipFromLParam(IntPtr lParam)
        {
            return new Point((short)((int)lParam & 0xffff), (short)((int)lParam >> 16)) / Scaling;
        }

        private Point PointFromLParam(IntPtr lParam)
        {
            return new Point((short)((int)lParam & 0xffff), (short)((int)lParam >> 16));
        }

        private Point ScreenToClient(Point point)
        {
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y);
        }

        private void ShowWindow(WindowState state)
        {
            UnmanagedMethods.ShowWindowCommand command;

            switch (state)
            {
                case WindowState.Minimized:
                    command = UnmanagedMethods.ShowWindowCommand.Minimize;
                    break;
                case WindowState.Maximized:
                    command = UnmanagedMethods.ShowWindowCommand.Maximize;
                    break;
                case WindowState.Normal:
                    command = UnmanagedMethods.ShowWindowCommand.Restore;
                    break;
                default:
                    throw new ArgumentException("Invalid WindowState.");
            }

            UnmanagedMethods.ShowWindow(_hwnd, command);
        }

    }
}
