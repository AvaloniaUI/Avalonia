using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
        private ExtendClientAreaChromeHints _extendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;

        public WlToplevel(AvaloniaWaylandPlatform platform) : base(platform)
        {
            _platform = platform;
        }

        public Func<WindowCloseReason, bool>? Closing { get; set; }

        public Action? GotInputWhenDisabled { get; set; }

        public Action<WindowState>? WindowStateChanged { get; set; }

        public Action<bool>? ExtendClientAreaToDecorationsChanged { get; set; }

        public bool IsClientAreaExtendedToDecorations { get; private set; }

        public bool NeedsManagedDecorations => IsClientAreaExtendedToDecorations && _extendClientAreaChromeHints.HasAnyFlag(ExtendClientAreaChromeHints.PreferSystemChrome | ExtendClientAreaChromeHints.SystemChrome);

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
            }
            else
            {
                IsClientAreaExtendedToDecorations = true;
                ExtendClientAreaToDecorationsChanged?.Invoke(IsClientAreaExtendedToDecorations);
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
            base.Hide();
            _xdgToplevel?.Dispose();
            _xdgToplevel = null;
            _toplevelDecoration?.Dispose();
            _toplevelDecoration = null;
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
            var decorations = enabled == SystemDecorations.Full;
            _extendClientAreaChromeHints = decorations ? ExtendClientAreaChromeHints.Default : ExtendClientAreaChromeHints.NoChrome;
            _toplevelDecoration?.SetMode(decorations ? ZxdgToplevelDecorationV1.ModeEnum.ServerSide : ZxdgToplevelDecorationV1.ModeEnum.ClientSide);
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
                _xdgToplevel?.SetMinSize((int)ClientSize.Width, (int)ClientSize.Height);
                _xdgToplevel?.SetMaxSize((int)ClientSize.Width, (int)ClientSize.Height);
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
            _toplevelDecoration?.SetMode(extendIntoClientAreaHint ?
                ZxdgToplevelDecorationV1.ModeEnum.ClientSide :
                ZxdgToplevelDecorationV1.ModeEnum.ServerSide);
        }

        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {
            _extendClientAreaChromeHints = hints;
            ExtendClientAreaToDecorationsChanged?.Invoke(IsClientAreaExtendedToDecorations);
        }

        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
            _extendedMargins = titleBarHeight is -1 ? s_windowDecorationThickness : new Thickness(0, titleBarHeight, 0, 0);
            ExtendClientAreaToDecorationsChanged?.Invoke(IsClientAreaExtendedToDecorations);
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
            PendingState.NeedsWindowDecoration = mode == ZxdgToplevelDecorationV1.ModeEnum.ClientSide;
        }

        public void OnHandle(ZxdgExportedV2 eventSender, string handle)
        {
            _storageProvider = new WlStorageProviderProxy(handle);
        }

        public override object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IStorageProvider))
                return _storageProvider;
            if (featureType == typeof(IClipboard))
                return AvaloniaLocator.Current.GetService<IClipboard>();
            return null;
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
            if (PendingState.NeedsWindowDecoration != IsClientAreaExtendedToDecorations)
            {
                IsClientAreaExtendedToDecorations = PendingState.NeedsWindowDecoration;
                ExtendClientAreaToDecorationsChanged?.Invoke(IsClientAreaExtendedToDecorations);
            }

            if (PendingState.Activated)
                Activated?.Invoke();

            if (AppliedState.WindowState != PendingState.WindowState)
                WindowStateChanged?.Invoke(PendingState.WindowState);

            base.ApplyConfigure();
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
