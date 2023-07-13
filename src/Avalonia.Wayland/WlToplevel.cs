using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using NWayland.Protocols.XdgDecorationUnstableV1;
using NWayland.Protocols.XdgForeignUnstableV2;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland
{
    internal class WlToplevel : WlWindow, IWindowImpl, XdgToplevel.IEvents, ZxdgToplevelDecorationV1.IEvents, ZxdgExportedV2.IEvents
    {
        private readonly AvaloniaWaylandPlatform _platform;

        private XdgToplevel? _xdgToplevel;
        private ZxdgToplevelDecorationV1? _toplevelDecoration;
        private ZxdgExportedV2? _exportedToplevel;
        private WlStorageProviderProxy? _storageProvider;
        private string? _title;
        private PixelSize _minSize;
        private PixelSize _maxSize;
        private bool _extendIntoClientArea;
        private SystemDecorations _systemDecorations = SystemDecorations.Full;

        public WlToplevel(AvaloniaWaylandPlatform platform) : base(platform)
        {
            _platform = platform;
        }

        public Func<WindowCloseReason, bool>? Closing { get; set; }

        public Action? GotInputWhenDisabled { get; set; }

        public Action<WindowState>? WindowStateChanged { get; set; }

        public Action<bool>? ExtendClientAreaToDecorationsChanged { get; set; }

        public Action<SystemDecorations>? RequestedManagedDecorationsChanged { get; set; }

        public bool IsClientAreaExtendedToDecorations => !AppliedState.HasWindowDecorations;

        public SystemDecorations RequestedManagedDecorations => AppliedState.HasWindowDecorations ? SystemDecorations.None : _systemDecorations;

        private Thickness _extendedMargins = s_windowDecorationThickness;
        public Thickness ExtendedMargins => IsClientAreaExtendedToDecorations ? _extendedMargins : default;

        public Thickness OffScreenMargin => default;

        public WindowState WindowState
        {
            get => AppliedState.WindowState;
            set
            {
                if (AppliedState.WindowState == value)
                    return;
                switch (value)
                {
                    case WindowState.Minimized:
                        _xdgToplevel?.SetMinimized();
                        break;
                    case WindowState.Maximized:
                        _xdgToplevel?.UnsetFullscreen();
                        _xdgToplevel?.SetMaximized();
                        break;
                    case WindowState.FullScreen:
                        _xdgToplevel?.SetFullscreen(WlOutput);
                        break;
                    case WindowState.Normal:
                        _xdgToplevel?.UnsetFullscreen();
                        _xdgToplevel?.UnsetMaximized();
                        break;
                }
            }
        }

        public override void Show(bool activate, bool isDialog)
        {
            _xdgToplevel = XdgSurface.GetToplevel();
            _xdgToplevel.Events = this;

            if (_platform.Options.AppId is not null)
                _xdgToplevel.SetAppId(_platform.Options.AppId);

            if (_title is not null)
                _xdgToplevel.SetTitle(_title);

            if (_platform.ZxdgDecorationManager is not null)
            {
                _toplevelDecoration = _platform.ZxdgDecorationManager.GetToplevelDecoration(_xdgToplevel);
                _toplevelDecoration.Events = this;
                var extend = _extendIntoClientArea || _systemDecorations != SystemDecorations.Full;
                var mode = extend ? ZxdgToplevelDecorationV1.ModeEnum.ClientSide : ZxdgToplevelDecorationV1.ModeEnum.ServerSide;
                _toplevelDecoration.SetMode(mode);
            }

            if (_platform.ZxdgExporter is not null)
            {
                _exportedToplevel = _platform.ZxdgExporter.ExportToplevel(WlSurface);
                _exportedToplevel.Events = this;
            }

            _xdgToplevel.SetMinSize(_minSize.Width, _minSize.Height);
            _xdgToplevel.SetMaxSize(_maxSize.Width, _maxSize.Height);

            base.Show(activate, isDialog);
        }

        public override void Hide()
        {
            WlSurface.Attach(null, 0, 0);
            _exportedToplevel?.Dispose();
            _exportedToplevel = null;
            _xdgToplevel?.Dispose();
            _xdgToplevel = null;
            _toplevelDecoration?.Dispose();
            _toplevelDecoration = null;
            _platform.WlDisplay.Roundtrip();
        }

        public void SetTitle(string? title)
        {
            _title = title;
            _xdgToplevel?.SetTitle(title ?? string.Empty);
        }

        public void SetParent(IWindowImpl parent)
        {
            if (parent is not WlToplevel wlToplevel || _xdgToplevel is null)
                return;
            _xdgToplevel.SetParent(wlToplevel._xdgToplevel);
            Parent = wlToplevel;
        }

        public void SetEnabled(bool enable) { }

        public void SetSystemDecorations(SystemDecorations enabled)
        {
            _systemDecorations = enabled;
            var extend = _extendIntoClientArea || _systemDecorations != SystemDecorations.Full;
            var mode = extend ? ZxdgToplevelDecorationV1.ModeEnum.ClientSide : ZxdgToplevelDecorationV1.ModeEnum.ServerSide;
            _toplevelDecoration?.SetMode(mode);
        }

        public void SetIcon(IWindowIconImpl? icon) { } // Impossible on Wayland, an AppId should be used instead.

        public void ShowTaskbarIcon(bool value) { } // Impossible on Wayland.

        public void CanResize(bool value)
        {
            if (value)
            {
                _xdgToplevel?.SetMinSize(_minSize.Width, _minSize.Height);
                _xdgToplevel?.SetMaxSize(_maxSize.Width, _maxSize.Height);
            }
            else
            {
                _xdgToplevel?.SetMinSize(AppliedState.Size.Width, AppliedState.Size.Height);
                _xdgToplevel?.SetMaxSize(AppliedState.Size.Width, AppliedState.Size.Height);
            }
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            _xdgToplevel?.Move(_platform.WlSeat, _platform.WlInputDevice.Serial);
            e.Pointer.Capture(null);
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
            _xdgToplevel?.Resize(_platform.WlSeat, _platform.WlInputDevice.Serial, ParseWindowEdges(edge));
            e.Pointer.Capture(null);
        }

        public void Move(PixelPoint point) { } // Impossible on Wayland.

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            var minX = double.IsInfinity(minSize.Width) ? 0 : (int)minSize.Width;
            var minY = double.IsInfinity(minSize.Height) ? 0 : (int)minSize.Height;
            var maxX = double.IsInfinity(maxSize.Width) ? 0 : (int)maxSize.Width;
            var maxY = double.IsInfinity(maxSize.Height) ? 0 : (int)maxSize.Height;
            _minSize = new PixelSize(minX, minY);
            _maxSize = new PixelSize(maxX, maxY);
            _xdgToplevel?.SetMinSize(_minSize.Width, _minSize.Height);
            _xdgToplevel?.SetMaxSize(_maxSize.Width, _maxSize.Height);
        }

        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {
            _extendIntoClientArea = extendIntoClientAreaHint;
            var mode = extendIntoClientAreaHint ? ZxdgToplevelDecorationV1.ModeEnum.ClientSide : ZxdgToplevelDecorationV1.ModeEnum.ServerSide;
            _toplevelDecoration?.SetMode(mode);
        }

        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints) { }

        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
            _extendedMargins = titleBarHeight is -1 ? s_windowDecorationThickness : new Thickness(0, titleBarHeight, 0, 0);
        }

        public void OnConfigure(XdgToplevel eventSender, int width, int height, ReadOnlySpan<XdgToplevel.StateEnum> states)
        {
            PendingState.WindowState = WindowState.Normal;
            foreach (var state in states)
            {
                switch (state)
                {
                    case XdgToplevel.StateEnum.Maximized:
                        PendingState.WindowState = WindowState.Maximized;
                        break;
                    case XdgToplevel.StateEnum.Fullscreen:
                        PendingState.WindowState = WindowState.FullScreen;
                        break;
                    case XdgToplevel.StateEnum.Activated:
                        PendingState.Activated = true;
                        break;
                }
            }

            var size = new PixelSize(width, height);
            if (size != default)
                PendingState.Size = size;
        }

        public void OnClose(XdgToplevel eventSender)
        {
            Closing?.Invoke(WindowCloseReason.WindowClosing);
        }

        public void OnConfigureBounds(XdgToplevel eventSender, int width, int height)
        {
            PendingState.Bounds = new PixelSize(width, height);
        }

        public void OnWmCapabilities(XdgToplevel eventSender, ReadOnlySpan<XdgToplevel.WmCapabilitiesEnum> capabilities) { }

        public void OnConfigure(ZxdgToplevelDecorationV1 eventSender, ZxdgToplevelDecorationV1.ModeEnum mode)
        {
            PendingState.HasWindowDecorations = mode == ZxdgToplevelDecorationV1.ModeEnum.ServerSide;
        }

        public void OnHandle(ZxdgExportedV2 eventSender, string handle)
        {
            _storageProvider = new WlStorageProviderProxy(handle);
        }

        public override object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IStorageProvider))
                return _storageProvider;
            return base.TryGetFeature(featureType);
        }

        public override void Dispose()
        {
            _exportedToplevel?.Dispose();
            _toplevelDecoration?.Dispose();
            _xdgToplevel?.Dispose();
            base.Dispose();
        }

        protected override void ApplyConfigure()
        {
            var windowStateChanged = PendingState.WindowState != AppliedState.WindowState;

            base.ApplyConfigure();

            if (AppliedState.Activated)
                Activated?.Invoke();
            if (windowStateChanged)
                WindowStateChanged?.Invoke(AppliedState.WindowState);

            ExtendClientAreaToDecorationsChanged?.Invoke(IsClientAreaExtendedToDecorations);
            RequestedManagedDecorationsChanged?.Invoke(RequestedManagedDecorations);
        }

        private static readonly Thickness s_windowDecorationThickness = new(0, 30, 0, 0);

        private static XdgToplevel.ResizeEdgeEnum ParseWindowEdges(WindowEdge windowEdge) => windowEdge switch
        {
            WindowEdge.North => XdgToplevel.ResizeEdgeEnum.Top,
            WindowEdge.NorthEast => XdgToplevel.ResizeEdgeEnum.TopRight,
            WindowEdge.East => XdgToplevel.ResizeEdgeEnum.Right,
            WindowEdge.SouthEast => XdgToplevel.ResizeEdgeEnum.BottomRight,
            WindowEdge.South => XdgToplevel.ResizeEdgeEnum.Bottom,
            WindowEdge.SouthWest => XdgToplevel.ResizeEdgeEnum.BottomLeft,
            WindowEdge.West => XdgToplevel.ResizeEdgeEnum.Left,
            WindowEdge.NorthWest => XdgToplevel.ResizeEdgeEnum.TopLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(windowEdge))
        };
    }
}
