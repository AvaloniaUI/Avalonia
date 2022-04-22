using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using NWayland.Interop;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgDecorationUnstableV1;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland
{
    public class WlWindow : IWindowImpl, WlSurface.IEvents, XdgWmBase.IEvents, XdgSurface.IEvents, XdgToplevel.IEvents, WlCallback.IEvents
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly WlScreens _wlScreens;
        private readonly WlSurface _wlSurface;
        private readonly XdgSurface _xdgSurface;
        private readonly XdgToplevel _xdgToplevel;
        private readonly ZxdgToplevelDecorationV1 _toplevelDecoration;
        private readonly WlInputDevice _wlInputDevice;

        private bool _active;

        public WlWindow(AvaloniaWaylandPlatform platform, IWindowImpl? popupParent)
        {
            _platform = platform;
            platform.XdgWmBase.Events = this;
            _wlScreens = new WlScreens(platform);
            _wlInputDevice = new WlInputDevice(platform);
            _wlSurface = platform.WlCompositor.CreateSurface();
            _wlSurface.Events = this;
            _xdgSurface = platform.XdgWmBase.GetXdgSurface(_wlSurface);
            _xdgSurface.Events = this;
            _xdgToplevel = _xdgSurface.GetToplevel();
            _xdgToplevel.Events = this;
            _toplevelDecoration = platform.ZxdgDecorationManager.GetToplevelDecoration(_xdgToplevel);

            if (popupParent is not null)
                SetParent(popupParent);

            platform.WlDisplay.Roundtrip();
            var screens = _wlScreens.AllScreens;
            ClientSize = screens.Count > 0
                ? new Size(screens[0].WorkingArea.Width * 0.75, screens[0].WorkingArea.Height * 0.7)
                : new Size(400, 600);

            _surfaces = new List<object>
            {
                new WlFramebufferSurface()
            };

            var glFeature = AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>();
            if (glFeature is EglPlatformOpenGlInterface egl)
            {
                var eglWindow = LibWayland.wl_egl_window_create(_wlSurface.Handle, (int)ClientSize.Width, (int)ClientSize.Height);
                Handle = new PlatformHandle(eglWindow, "EGL_WINDOW");
                _surfaces.Add(new EglGlPlatformSurface(egl, new SurfaceInfo(this)));
            }
        }

        private sealed class SurfaceInfo : EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo
        {
            private readonly WlWindow _wlWindow;

            public SurfaceInfo(WlWindow wlWindow)
            {
                _wlWindow = wlWindow;
            }

            public IntPtr Handle => _wlWindow.Handle.Handle;
            public PixelSize Size => new((int)_wlWindow.ClientSize.Width, (int)_wlWindow.ClientSize.Height);
            public double Scaling => _wlWindow.RenderScaling;
        }

        public void Dispose()
        {
            _toplevelDecoration.Dispose();
            _xdgToplevel.Dispose();
            _xdgSurface.Dispose();
            _wlSurface.Dispose();
            _wlScreens.Dispose();
            if (Handle.Handle != IntPtr.Zero)
                LibWayland.wl_egl_window_destroy(Handle.Handle);
        }

        public Size ClientSize { get; private set; }
        public Size? FrameSize { get; }
        public double RenderScaling { get; private set; } = 1;

        private readonly List<object> _surfaces;
        public IEnumerable<object> Surfaces => _surfaces;

        public Action<RawInputEventArgs>? Input
        {
            get => _wlInputDevice.Input;
            set => _wlInputDevice.Input = value;
        }

        public Action<Rect>? Paint { get; set; }
        public Action<Size, PlatformResizeReason>? Resized { get; set; }
        public Action<double>? ScalingChanged { get; set; }
        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
            var customRendererFactory = AvaloniaLocator.Current.GetService<IRendererFactory>();

            if (customRendererFactory != null)
                return customRendererFactory.Create(root, loop);

            return _platform.Options.UseDeferredRendering
                ? new DeferredRenderer(root, loop) { RenderOnlyOnRenderThread = true }
                : new ImmediateRenderer(root);
        }

        public void Invalidate(Rect rect) => _wlSurface.Damage((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

        public void SetInputRoot(IInputRoot inputRoot) => _wlInputDevice.InputRoot = inputRoot;

        public Point PointToClient(PixelPoint point) => new(point.X, point.Y);

        public PixelPoint PointToScreen(Point point) => new((int)point.X, (int)point.Y);

        public void SetCursor(ICursorImpl? cursor)
        {
            if (cursor is not WlCursorFactory.WlCursor wlCursor) return;
        }

        public Action? Closed { get; set; }
        public Action? LostFocus { get; set; }
        public IMouseDevice MouseDevice { get; } = new MouseDevice();

        public IPopupImpl? CreatePopup()
        {
            return null;
        }

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {

        }

        public WindowTransparencyLevel TransparencyLevel { get; }
        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; }

        public void Show(bool activate, bool isDialog)
        {
            _wlSurface.Commit();
            _platform.WlDisplay.Sync().Events = this;
        }

        public void Hide()
        {

        }

        public double DesktopScaling => RenderScaling;
        public PixelPoint Position { get; private set; }
        public Action<PixelPoint>? PositionChanged { get; set; }
        public void Activate()
        {

        }

        public Action? Deactivated { get; set; }
        public Action? Activated { get; set; }
        public IPlatformHandle Handle { get; }
        public Size MaxAutoSizeHint { get; }
        public void SetTopmost(bool value)
        {

        }

        public IScreenImpl Screen => _wlScreens;

        private WindowState _windowState;
        public WindowState WindowState
        {
            get => _windowState;
            set
            {
                if (_windowState == value) return;
                switch (value)
                {
                    case WindowState.Minimized:
                        _xdgToplevel.SetMinimized();
                        break;
                    case WindowState.Maximized:
                        _xdgToplevel.SetMaximized();
                        break;
                    case WindowState.FullScreen:
                        _xdgToplevel.SetFullscreen(null);
                        break;
                    case WindowState.Normal when _windowState == WindowState.Maximized:
                        _xdgToplevel.UnsetMaximized();
                        break;
                    case WindowState.Normal when _windowState == WindowState.FullScreen:
                        _xdgToplevel.UnsetFullscreen();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }

        public Action<WindowState> WindowStateChanged { get; set; }

        public void SetTitle(string? title) => _xdgToplevel.SetTitle(title);

        public void SetParent(IWindowImpl parent)
        {
            if (parent is WlWindow wlWindow)
                _xdgToplevel.SetParent(wlWindow._xdgToplevel);
        }

        public void SetEnabled(bool enable)
        {

        }

        public Action GotInputWhenDisabled { get; set; }

        public void SetSystemDecorations(SystemDecorations enabled)
        {
            switch (enabled)
            {
                case SystemDecorations.Full:
                    _toplevelDecoration.SetMode(ZxdgToplevelDecorationV1.ModeEnum.ServerSide);
                    break;
                case SystemDecorations.None:
                case SystemDecorations.BorderOnly:
                    _toplevelDecoration.SetMode(ZxdgToplevelDecorationV1.ModeEnum.ClientSide);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(enabled), enabled, null);
            }
        }

        public void SetIcon(IWindowIconImpl? icon) { } // impossible on wayland

        public void ShowTaskbarIcon(bool value)
        {

        }

        public void CanResize(bool value)
        {

        }

        public Func<bool>? Closing { get; set; }
        public bool IsClientAreaExtendedToDecorations { get; }
        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }
        public bool NeedsManagedDecorations { get; }
        public Thickness ExtendedMargins { get; }
        public Thickness OffScreenMargin { get; }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {

        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {

        }

        public void Resize(Size clientSize, PlatformResizeReason reason = PlatformResizeReason.Application)
        {
            if (clientSize == ClientSize) return;
            LibWayland.wl_egl_window_resize(Handle.Handle, (int)clientSize.Width, (int)clientSize.Height, 0, 0);
            ClientSize = clientSize;
        }

        public void Move(PixelPoint point) { }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _xdgToplevel.SetMinSize((int)minSize.Width, (int)minSize.Height);
            _xdgToplevel.SetMaxSize((int)maxSize.Width, (int)maxSize.Height);
        }

        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint) =>
            _toplevelDecoration.SetMode(extendIntoClientAreaHint ? ZxdgToplevelDecorationV1.ModeEnum.ClientSide : ZxdgToplevelDecorationV1.ModeEnum.ServerSide);

        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints) { }

        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight) { }

        public IPopupPositioner PopupPositioner { get; }
        public void SetWindowManagerAddShadowHint(bool enabled)
        {

        }

        void XdgToplevel.IEvents.OnConfigure(XdgToplevel eventSender, int width, int height, ReadOnlySpan<XdgToplevel.StateEnum> states)
        {
            var newSize = new Size(width, height) * RenderScaling;
            if (newSize == Size.Empty) return;
            if (newSize != ClientSize)
            {
                ClientSize = newSize;
                LibWayland.wl_egl_window_resize(Handle.Handle, (int)newSize.Width, (int)newSize.Height, 0, 0);
                Resized?.Invoke(ClientSize, PlatformResizeReason.User);
            }

            if (states.Length == 0 && _active)
            {
                Deactivated?.Invoke();
                _active = false;
                return;
            }

            _windowState = WindowState.Normal;
            foreach (var state in states)
            {
                switch (state)
                {
                    case XdgToplevel.StateEnum.Maximized:
                        _windowState = WindowState.Maximized;
                        break;
                    case XdgToplevel.StateEnum.Fullscreen:
                        _windowState = WindowState.FullScreen;
                        break;
                    case XdgToplevel.StateEnum.Activated when !_active:
                        Activated?.Invoke();
                        _active = true;
                        break;
                }
            }

            Dispatcher.UIThread.RunJobs(DispatcherPriority.Layout);

            _wlSurface.Commit();
            _platform.WlDisplay.DispatchPending();
            _platform.WlDisplay.Sync().Events = this;
        }

        void XdgToplevel.IEvents.OnClose(XdgToplevel eventSender)
        {
            if (Closing?.Invoke() is not true)
                Closed?.Invoke();
        }

        void WlCallback.IEvents.OnDone(WlCallback eventSender, uint callbackData)
        {
            Paint?.Invoke(new Rect());
            _wlSurface.Frame().Events = this;
            NWayland.Interop.LibWayland.wl_callback_destroy(eventSender.Handle);
        }

        void XdgSurface.IEvents.OnConfigure(XdgSurface eventSender, uint serial) => _xdgSurface.AckConfigure(serial);

        void XdgWmBase.IEvents.OnPing(XdgWmBase eventSender, uint serial) => _platform.XdgWmBase.Pong(serial);

        void WlSurface.IEvents.OnEnter(WlSurface eventSender, WlOutput output)
        {
            var screen = _wlScreens.ScreenFromOutput(output);
            if (Math.Abs(screen.PixelDensity - RenderScaling) < double.Epsilon) return;
            RenderScaling = screen.PixelDensity;
            ScalingChanged?.Invoke(RenderScaling);
        }

        void WlSurface.IEvents.OnLeave(WlSurface eventSender, WlOutput output) { }
    }
}
