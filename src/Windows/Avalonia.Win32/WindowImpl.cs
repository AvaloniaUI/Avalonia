using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    /// <summary>
    /// Window implementation for Win32 platform.
    /// </summary>
    public partial class WindowImpl : IWindowImpl, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
    {
        private static readonly List<WindowImpl> s_instances = new List<WindowImpl>();

        private static readonly IntPtr DefaultCursor = LoadCursor(
            IntPtr.Zero, new IntPtr((int)UnmanagedMethods.Cursor.IDC_ARROW));

        private static readonly Dictionary<WindowEdge, HitTestValues> s_edgeLookup =
            new Dictionary<WindowEdge, HitTestValues>
            {
                { WindowEdge.East, HitTestValues.HTRIGHT },
                { WindowEdge.North, HitTestValues.HTTOP },
                { WindowEdge.NorthEast, HitTestValues.HTTOPRIGHT },
                { WindowEdge.NorthWest, HitTestValues.HTTOPLEFT },
                { WindowEdge.South, HitTestValues.HTBOTTOM },
                { WindowEdge.SouthEast, HitTestValues.HTBOTTOMRIGHT },
                { WindowEdge.SouthWest, HitTestValues.HTBOTTOMLEFT },
                { WindowEdge.West, HitTestValues.HTLEFT }
            };

        private SavedWindowInfo _savedWindowInfo;
        private bool _isFullScreenActive;

#if USE_MANAGED_DRAG
        private readonly ManagedWindowResizeDragHelper _managedDrag;
#endif

        private const WindowStyles WindowStateMask = (WindowStyles.WS_MAXIMIZE | WindowStyles.WS_MINIMIZE);
        private readonly TouchDevice _touchDevice;
        private readonly MouseDevice _mouseDevice;
        private readonly ManagedDeferredRendererLock _rendererLock;
        private readonly FramebufferManager _framebuffer;
        private readonly IGlPlatformSurface _gl;

        private WndProc _wndProcDelegate;
        private string _className;
        private IntPtr _hwnd;
        private bool _multitouch;
        private IInputRoot _owner;
        private WindowProperties _windowProperties;
        private bool _trackingMouse;
        private bool _topmost;
        private double _scaling = 1;
        private WindowState _showWindowState;
        private WindowState _lastWindowState;
        private OleDropTarget _dropTarget;
        private Size _minSize;
        private Size _maxSize;
        private WindowImpl _parent;

        public WindowImpl()
        {
            _touchDevice = new TouchDevice();
            _mouseDevice = new WindowsMouseDevice();

#if USE_MANAGED_DRAG
            _managedDrag = new ManagedWindowResizeDragHelper(this, capture =>
            {
                if (capture)
                    UnmanagedMethods.SetCapture(Handle.Handle);
                else
                    UnmanagedMethods.ReleaseCapture();
            });
#endif

            _windowProperties = new WindowProperties
            {
                ShowInTaskbar = false,
                IsResizable = true,
                Decorations = SystemDecorations.Full
            };
            _rendererLock = new ManagedDeferredRendererLock();


            CreateWindow();
            _framebuffer = new FramebufferManager(_hwnd);

            if (Win32GlManager.EglFeature != null)
                _gl = new EglGlPlatformSurface(Win32GlManager.EglFeature.DeferredContext, this);

            Screen = new ScreenImpl();

            s_instances.Add(this);
        }

        public Action Activated { get; set; }

        public Func<bool> Closing { get; set; }

        public Action Closed { get; set; }

        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public Action<PixelPoint> PositionChanged { get; set; }

        public Action<WindowState> WindowStateChanged { get; set; }

        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public Thickness BorderThickness
        {
            get
            {
                if (HasFullDecorations)
                {
                    var style = GetStyle();
                    var exStyle = GetExtendedStyle();

                    var padding = new RECT();

                    if (AdjustWindowRectEx(ref padding, (uint)style, false, (uint)exStyle))
                    {
                        return new Thickness(-padding.left, -padding.top, padding.right, padding.bottom);
                    }
                    else
                    {
                        throw new Win32Exception();
                    }
                }
                else
                {
                    return new Thickness();
                }
            }
        }

        public double Scaling => _scaling;

        public Size ClientSize
        {
            get
            {
                GetClientRect(_hwnd, out var rect);

                return new Size(rect.right, rect.bottom) / Scaling;
            }
        }

        public IScreenImpl Screen { get; }

        public IPlatformHandle Handle { get; private set; }

        public Size MaxClientSize
        {
            get
            {
                return (new Size(
                            GetSystemMetrics(SystemMetric.SM_CXMAXTRACK),
                            GetSystemMetrics(SystemMetric.SM_CYMAXTRACK))
                        - BorderThickness) / Scaling;
            }
        }

        public IMouseDevice MouseDevice => _mouseDevice;

        public WindowState WindowState
        {
            get
            {
                var placement = default(WINDOWPLACEMENT);
                GetWindowPlacement(_hwnd, ref placement);

                return placement.ShowCmd switch
                {
                    ShowWindowCommand.Maximize => WindowState.Maximized,
                    ShowWindowCommand.Minimize => WindowState.Minimized,
                    _ => WindowState.Normal
                };
            }

            set
            {
                if (IsWindowVisible(_hwnd))
                {
                    ShowWindow(value);
                }
                else
                {
                    _showWindowState = value;
                }
            }
        }

        public WindowTransparencyLevel TransparencyLevel { get; private set; }

        public void SetTransparencyLevelHint (WindowTransparencyLevel transparencyLevel)
        {
            TransparencyLevel = EnableBlur(transparencyLevel);
        }

        private WindowTransparencyLevel EnableBlur(WindowTransparencyLevel transparencyLevel)
        {
            bool canUseTransparency = false;
            bool canUseAcrylic = false;

            if (Win32Platform.WindowsVersion.Major >= 10)
            {
                canUseTransparency = true;

                if (Win32Platform.WindowsVersion.Major > 10 || Win32Platform.WindowsVersion.Build >= 19628)
                {
                    canUseAcrylic = true;
                }
            }

            if (!canUseTransparency || DwmIsCompositionEnabled(out var compositionEnabled) != 0 || !compositionEnabled)
            {
                return WindowTransparencyLevel.None;
            }

            var accent = new AccentPolicy();
            var accentStructSize = Marshal.SizeOf(accent);

            if(transparencyLevel == WindowTransparencyLevel.AcrylicBlur && !canUseAcrylic)
            {
                transparencyLevel = WindowTransparencyLevel.Blur;
            }

            switch (transparencyLevel)
            {
                default:
                case WindowTransparencyLevel.None:
                    accent.AccentState = AccentState.ACCENT_DISABLED;
                    break;

                case WindowTransparencyLevel.Transparent:
                    accent.AccentState = AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT;
                    break;

                case WindowTransparencyLevel.Blur:
                    accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                    break;

                case WindowTransparencyLevel.AcrylicBlur:
                case (WindowTransparencyLevel.AcrylicBlur + 1): // hack-force acrylic.
                    accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLIC;
                    transparencyLevel = WindowTransparencyLevel.AcrylicBlur;
                    break;
            }

            accent.AccentFlags = 2;
            accent.GradientColor = 0x01000000;

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(_hwnd, ref data);

            Marshal.FreeHGlobal(accentPtr);            

            return transparencyLevel;
        }

        public IEnumerable<object> Surfaces => new object[] { Handle, _gl, _framebuffer };

        public PixelPoint Position
        {
            get
            {
                GetWindowRect(_hwnd, out var rc);

                return new PixelPoint(rc.left, rc.top);
            }
            set
            {
                SetWindowPos(
                    Handle.Handle,
                    IntPtr.Zero,
                    value.X,
                    value.Y,
                    0,
                    0,
                    SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }

        private bool HasFullDecorations => _windowProperties.Decorations == SystemDecorations.Full;

        public void Move(PixelPoint point) => Position = point;

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _minSize = minSize;
            _maxSize = maxSize;
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
            var customRendererFactory = AvaloniaLocator.Current.GetService<IRendererFactory>();

            if (customRendererFactory != null)
                return customRendererFactory.Create(root, loop);

            return Win32Platform.UseDeferredRendering ?
                (IRenderer)new DeferredRenderer(root, loop, rendererLock: _rendererLock) :
                new ImmediateRenderer(root);
        }

        public void Resize(Size value)
        {
            int requestedClientWidth = (int)(value.Width * Scaling);
            int requestedClientHeight = (int)(value.Height * Scaling);

            GetClientRect(_hwnd, out var clientRect);

            // do comparison after scaling to avoid rounding issues
            if (requestedClientWidth != clientRect.Width || requestedClientHeight != clientRect.Height)
            {
                GetWindowRect(_hwnd, out var windowRect);

                SetWindowPos(
                    _hwnd,
                    IntPtr.Zero,
                    0,
                    0,
                    requestedClientWidth + (windowRect.Width - clientRect.Width),
                    requestedClientHeight + (windowRect.Height - clientRect.Height),
                    SetWindowPosFlags.SWP_RESIZE);
            }
        }

        public void Activate()
        {
            SetActiveWindow(_hwnd);
        }

        public IPopupImpl CreatePopup() => Win32Platform.UseOverlayPopups ? null : new PopupImpl(this);

        public void Dispose()
        {
            if (_dropTarget != null)
            {
                OleContext.Current?.UnregisterDragDrop(Handle);
                _dropTarget = null;
            }

            if (_hwnd != IntPtr.Zero)
            {
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }

            if (_className != null)
            {
                UnregisterClass(_className, GetModuleHandle(null));
                _className = null;
            }
        }

        public void Invalidate(Rect rect)
        {
            var scaling = Scaling;
            var r = new RECT
            {
                left = (int)Math.Floor(rect.X * scaling),
                top = (int)Math.Floor(rect.Y * scaling),
                right = (int)Math.Ceiling(rect.Right * scaling),
                bottom = (int)Math.Ceiling(rect.Bottom * scaling),
            };

            InvalidateRect(_hwnd, ref r, false);
        }

        public Point PointToClient(PixelPoint point)
        {
            var p = new POINT { X = point.X, Y = point.Y };
            UnmanagedMethods.ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y) / Scaling;
        }

        public PixelPoint PointToScreen(Point point)
        {
            point *= Scaling;
            var p = new POINT { X = (int)point.X, Y = (int)point.Y };
            ClientToScreen(_hwnd, ref p);
            return new PixelPoint(p.X, p.Y);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _owner = inputRoot;
            CreateDropTarget();
        }

        public void Hide()
        {
            UnmanagedMethods.ShowWindow(_hwnd, ShowWindowCommand.Hide);
        }

        public virtual void Show()
        {
            SetWindowLongPtr(_hwnd, (int)WindowLongParam.GWL_HWNDPARENT, _parent != null ? _parent._hwnd : IntPtr.Zero);
            ShowWindow(_showWindowState);
        }

        public Action GotInputWhenDisabled { get; set; }

        public void SetParent(IWindowImpl parent)
        {
            _parent = (WindowImpl)parent;
            SetWindowLongPtr(_hwnd, (int)WindowLongParam.GWL_HWNDPARENT, _parent._hwnd);
        }

        public void SetEnabled(bool enable) => EnableWindow(_hwnd, enable);

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            _mouseDevice.Capture(null);
            DefWindowProc(_hwnd, (int)WindowsMessage.WM_NCLBUTTONDOWN,
                new IntPtr((int)HitTestValues.HTCAPTION), IntPtr.Zero);
            e.Pointer.Capture(null);
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
#if USE_MANAGED_DRAG
            _managedDrag.BeginResizeDrag(edge, ScreenToClient(MouseDevice.Position.ToPoint(_scaling)));
#else
            _mouseDevice.Capture(null);
            DefWindowProc(_hwnd, (int)WindowsMessage.WM_NCLBUTTONDOWN,
                new IntPtr((int)s_edgeLookup[edge]), IntPtr.Zero);
#endif
        }

        public void SetTitle(string title)
        {
            SetWindowText(_hwnd, title);
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            var hCursor = cursor?.Handle ?? DefaultCursor;
            SetClassLong(_hwnd, ClassLongIndex.GCLP_HCURSOR, hCursor);

            if (_owner.IsPointerOver)
            {
                UnmanagedMethods.SetCursor(hCursor);
            }
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            var impl = (IconImpl)icon;
            var hIcon = impl?.HIcon ?? IntPtr.Zero;
            PostMessage(_hwnd, (int)WindowsMessage.WM_SETICON,
                new IntPtr((int)Icons.ICON_BIG), hIcon);
        }

        public void ShowTaskbarIcon(bool value)
        {
            var newWindowProperties = _windowProperties;

            newWindowProperties.ShowInTaskbar = value;

            UpdateWindowProperties(newWindowProperties);
        }

        public void CanResize(bool value)
        {
            var newWindowProperties = _windowProperties;

            newWindowProperties.IsResizable = value;

            UpdateWindowProperties(newWindowProperties);
        }

        public void SetSystemDecorations(SystemDecorations value)
        {
            var newWindowProperties = _windowProperties;

            newWindowProperties.Decorations = value;

            UpdateWindowProperties(newWindowProperties);
        }

        public void SetTopmost(bool value)
        {
            if (value == _topmost)
            {
                return;
            }

            IntPtr hWndInsertAfter = value ? WindowPosZOrder.HWND_TOPMOST : WindowPosZOrder.HWND_NOTOPMOST;
            SetWindowPos(_hwnd,
                hWndInsertAfter,
                0, 0, 0, 0,
                SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);

            _topmost = value;
        }

        protected virtual IntPtr CreateWindowOverride(ushort atom)
        {
            return CreateWindowEx(
                0,
                atom,
                null,
                (int)WindowStyles.WS_OVERLAPPEDWINDOW,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        private void CreateWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = WndProc;

            _className = $"Avalonia-{Guid.NewGuid().ToString()}";

            // Unique DC helps with performance when using Gpu based rendering
            const ClassStyles windowClassStyle = ClassStyles.CS_OWNDC | ClassStyles.CS_HREDRAW | ClassStyles.CS_VREDRAW;

            var wndClassEx = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                style = (int)windowClassStyle,
                lpfnWndProc = _wndProcDelegate,
                hInstance = GetModuleHandle(null),
                hCursor = DefaultCursor,
                hbrBackground = IntPtr.Zero,
                lpszClassName = _className
            };

            ushort atom = RegisterClassEx(ref wndClassEx);

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

            _multitouch = Win32Platform.Options.EnableMultitouch ?? false;

            if (_multitouch)
            {
                RegisterTouchWindow(_hwnd, 0);
            }

            if (ShCoreAvailable)
            {
                var monitor = MonitorFromWindow(
                    _hwnd,
                    MONITOR.MONITOR_DEFAULTTONEAREST);

                if (GetDpiForMonitor(
                    monitor,
                    MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                    out var dpix,
                    out var dpiy) == 0)
                {
                    _scaling = dpix / 96.0;
                }
            }
        }

        private void CreateDropTarget()
        {
            var odt = new OleDropTarget(this, _owner);

            if (OleContext.Current?.RegisterDragDrop(Handle, odt) ?? false)
            {
                _dropTarget = odt;
            }
        }

        /// <summary>
        /// Ported from https://github.com/chromium/chromium/blob/master/ui/views/win/fullscreen_handler.cc
        /// Method must only be called from inside UpdateWindowProperties.
        /// </summary>
        /// <param name="fullscreen"></param>
        private void SetFullScreen(bool fullscreen)
        {
            if (fullscreen)
            {
                GetWindowRect(_hwnd, out var windowRect);
                _savedWindowInfo.WindowRect = windowRect;

                var current = GetStyle();
                var currentEx = GetExtendedStyle();

                _savedWindowInfo.Style = current;
                _savedWindowInfo.ExStyle = currentEx;

                // Set new window style and size.
                SetStyle(current & ~(WindowStyles.WS_CAPTION | WindowStyles.WS_THICKFRAME), false);
                SetExtendedStyle(currentEx & ~(WindowStyles.WS_EX_DLGMODALFRAME | WindowStyles.WS_EX_WINDOWEDGE | WindowStyles.WS_EX_CLIENTEDGE | WindowStyles.WS_EX_STATICEDGE), false);

                // On expand, if we're given a window_rect, grow to it, otherwise do
                // not resize.
                MONITORINFO monitor_info = MONITORINFO.Create();
                GetMonitorInfo(MonitorFromWindow(_hwnd, MONITOR.MONITOR_DEFAULTTONEAREST), ref monitor_info);

                var window_rect = monitor_info.rcMonitor.ToPixelRect();

                SetWindowPos(_hwnd, IntPtr.Zero, window_rect.X, window_rect.Y,
                             window_rect.Width, window_rect.Height,
                             SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_FRAMECHANGED);

                _isFullScreenActive = true;
            }
            else
            {
                // Reset original window style and size.  The multiple window size/moves
                // here are ugly, but if SetWindowPos() doesn't redraw, the taskbar won't be
                // repainted.  Better-looking methods welcome.
                _isFullScreenActive = false;

                var windowStates = GetWindowStateStyles();
                SetStyle((_savedWindowInfo.Style & ~WindowStateMask) | windowStates, false);
                SetExtendedStyle(_savedWindowInfo.ExStyle, false);

                // On restore, resize to the previous saved rect size.
                var new_rect = _savedWindowInfo.WindowRect.ToPixelRect();

                SetWindowPos(_hwnd, IntPtr.Zero, new_rect.X, new_rect.Y, new_rect.Width,
                             new_rect.Height,
                            SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_FRAMECHANGED);

                UpdateWindowProperties(_windowProperties, true);
            }

            TaskBarList.MarkFullscreen(_hwnd, fullscreen);
        }

        private void ShowWindow(WindowState state)
        {
            ShowWindowCommand command;

            var newWindowProperties = _windowProperties;

            switch (state)
            {
                case WindowState.Minimized:
                    newWindowProperties.IsFullScreen = false;
                    command = ShowWindowCommand.Minimize;
                    break;
                case WindowState.Maximized:
                    newWindowProperties.IsFullScreen = false;
                    command = ShowWindowCommand.Maximize;
                    break;

                case WindowState.Normal:
                    newWindowProperties.IsFullScreen = false;
                    command = ShowWindowCommand.Restore;
                    break;

                case WindowState.FullScreen:
                    newWindowProperties.IsFullScreen = true;
                    UpdateWindowProperties(newWindowProperties);
                    return;

                default:
                    throw new ArgumentException("Invalid WindowState.");
            }

            UpdateWindowProperties(newWindowProperties);

            UnmanagedMethods.ShowWindow(_hwnd, command);

            if (state == WindowState.Maximized)
            {
                MaximizeWithoutCoveringTaskbar();
            }

            if (!Design.IsDesignMode)
            {
                SetFocus(_hwnd);
            }
        }

        private void MaximizeWithoutCoveringTaskbar()
        {
            IntPtr monitor = MonitorFromWindow(_hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = MONITORINFO.Create();

                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var x = monitorInfo.rcWork.left;
                    var y = monitorInfo.rcWork.top;
                    var cx = Math.Abs(monitorInfo.rcWork.right - x);
                    var cy = Math.Abs(monitorInfo.rcWork.bottom - y);

                    SetWindowPos(_hwnd, WindowPosZOrder.HWND_NOTOPMOST, x, y, cx, cy, SetWindowPosFlags.SWP_SHOWWINDOW);
                }
            }
        }        

        private WindowStyles GetWindowStateStyles()
        {
            return GetStyle() & WindowStateMask;
        }

        private WindowStyles GetStyle()
        {
            if (_isFullScreenActive)
            {
                return _savedWindowInfo.Style;
            }
            else
            {
                return (WindowStyles)GetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE);
            }
        }

        private WindowStyles GetExtendedStyle()
        {
            if (_isFullScreenActive)
            {
                return _savedWindowInfo.ExStyle;
            }
            else
            {
                return (WindowStyles)GetWindowLong(_hwnd, (int)WindowLongParam.GWL_EXSTYLE);
            }
        }

        private void SetStyle(WindowStyles style, bool save = true)
        {
            if (save)
            {
                _savedWindowInfo.Style = style;
            }

            if (!_isFullScreenActive)
            {
                SetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE, (uint)style);
            }
        }

        private void SetExtendedStyle(WindowStyles style, bool save = true)
        {
            if (save)
            {
                _savedWindowInfo.ExStyle = style;
            }

            if (!_isFullScreenActive)
            {
                SetWindowLong(_hwnd, (int)WindowLongParam.GWL_EXSTYLE, (uint)style);
            }
        }

        private void UpdateWindowProperties(WindowProperties newProperties, bool forceChanges = false)
        {
            var oldProperties = _windowProperties;

            // Calling SetWindowPos will cause events to be sent and we need to respond
            // according to the new values already.
            _windowProperties = newProperties;

            if ((oldProperties.ShowInTaskbar != newProperties.ShowInTaskbar) || forceChanges)
            {
                var exStyle = GetExtendedStyle();

                if (newProperties.ShowInTaskbar)
                {
                    exStyle |= WindowStyles.WS_EX_APPWINDOW;
                }
                else
                {
                    exStyle &= ~WindowStyles.WS_EX_APPWINDOW;
                }

                SetExtendedStyle(exStyle);

                // TODO: To hide non-owned window from taskbar we need to parent it to a hidden window.
                // Otherwise it will still show in the taskbar.
            }

            if ((oldProperties.IsResizable != newProperties.IsResizable) || forceChanges)
            {
                var style = GetStyle();

                if (newProperties.IsResizable)
                {
                    style |= WindowStyles.WS_SIZEFRAME;
                }
                else
                {
                    style &= ~WindowStyles.WS_SIZEFRAME;
                }

                SetStyle(style);
            }

            if (oldProperties.IsFullScreen != newProperties.IsFullScreen)
            {
                SetFullScreen(newProperties.IsFullScreen);
            }

            if ((oldProperties.Decorations != newProperties.Decorations) || forceChanges)
            {
                var style = GetStyle();

                const WindowStyles fullDecorationFlags = WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU;

                if (newProperties.Decorations == SystemDecorations.Full)
                {
                    style |= fullDecorationFlags;
                }
                else
                {
                    style &= ~fullDecorationFlags;
                }

                SetStyle(style);

                if (!_isFullScreenActive)
                {
                    var margin = newProperties.Decorations == SystemDecorations.BorderOnly ? 1 : 0;

                    var margins = new MARGINS
                    {
                        cyBottomHeight = margin,
                        cxRightWidth = margin,
                        cxLeftWidth = margin,
                        cyTopHeight = margin
                    };

                    DwmExtendFrameIntoClientArea(_hwnd, ref margins);

                    GetClientRect(_hwnd, out var oldClientRect);
                    var oldClientRectOrigin = new POINT();
                    ClientToScreen(_hwnd, ref oldClientRectOrigin);
                    oldClientRect.Offset(oldClientRectOrigin);

                    var newRect = oldClientRect;

                    if (newProperties.Decorations == SystemDecorations.Full)
                    {
                        AdjustWindowRectEx(ref newRect, (uint)style, false, (uint)GetExtendedStyle());
                    }

                    SetWindowPos(_hwnd, IntPtr.Zero, newRect.left, newRect.top, newRect.Width, newRect.Height,
                        SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE |
                        SetWindowPosFlags.SWP_FRAMECHANGED);
                }
            }
        }

#if USE_MANAGED_DRAG
        private Point ScreenToClient(Point point)
        {
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y);
        }
#endif

        PixelSize EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Size
        {
            get
            {
                GetClientRect(_hwnd, out var rect);

                return new PixelSize(
                    Math.Max(1, rect.right - rect.left),
                    Math.Max(1, rect.bottom - rect.top));
            }
        }

        IntPtr EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Handle => Handle.Handle;

        private struct SavedWindowInfo
        {
            public WindowStyles Style { get; set; }
            public WindowStyles ExStyle { get; set; }
            public RECT WindowRect { get; set; }
        };

        private struct WindowProperties
        {
            public bool ShowInTaskbar;
            public bool IsResizable;
            public SystemDecorations Decorations;
            public bool IsFullScreen;
        }
    }
}
