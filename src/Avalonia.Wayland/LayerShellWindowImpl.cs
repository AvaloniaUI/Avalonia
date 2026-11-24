using System;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Dialogs;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland;

/// <summary>
/// Window implementation backed by <c>zwlr_layer_surface_v1</c> instead of an
/// xdg toplevel. Used for shell surfaces that must not appear in the taskbar
/// or take keyboard focus: the compositor owns their position, they have no
/// decorations, and they cannot be moved or resized interactively.
/// </summary>
internal sealed class LayerShellWindowImpl : WindowBaseImpl, IWindowImpl
{
    private readonly LayerShellOptions _options;
    private LayerSurfaceCreateResult? _handle;
    private WLayerSurfaceProxy? _surfaceProxy;
    private FallbackStorageProvider? _storageProvider;
    private WindowState _windowState = WindowState.Normal;
    private string? _title;
    private string? _appId;
    private int _resizeDepth;
    private Size _requestedSize;

    public LayerShellWindowImpl(WaylandWorkerClient client, LayerShellOptions options)
        : base(client)
    {
        _options = options;
        _requestedSize = new Size(
            Math.Max(1, options.Width),
            Math.Max(1, options.Height));
    }

    public override IPlatformRenderSurface[] Surfaces => _handle?.GetRenderSurfaces() ?? [];

    internal override WXdgShellSurfaceProxy? SurfaceProxy => null;

    internal override WSurfaceProxy? PopupParentProxy => _surfaceProxy;

    internal override WSurfaceProxy? SurfaceProxyForCursor => _surfaceProxy;

    public override IPopupImpl? CreatePopup() => new PopupImpl(Client, this);

    public override Size? FrameSize => null;

    public override Size MaxAutoSizeHint => new(800, 600);

    public override void Show(bool activate, bool isDialog)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(LayerShellWindowImpl));

        if (CurrentSink is null)
            CurrentSink = new Sink(this);

        Client.AnyThreadWakeupRenderLoop();
    }

    private void ApplyConfigure(LayerConfigure configure)
    {
        var size = new Size(
            Math.Max(1, configure.Width),
            Math.Max(1, configure.Height));

        if (size == ClientSize)
            return;

        ClientSize = size;
        Resized?.Invoke(size, WindowResizeReason.Layout);
    }

    // ---- xdg-only concepts: no-op on a layer surface --------------------

    public WindowState WindowState
    {
        get => _windowState;
        set { }
    }

    public bool WindowStateGetterIsUsable => false;

    public Action<WindowState>? WindowStateChanged { get; set; }

    public void SetTitle(string? title) => _title = title;

    public void SetAppId(string? appId) => _appId = appId;

    public void SetParent(IWindowImpl? parent) { }

    public void SetEnabled(bool enable) => IsEnabled = enable;

    public Action? GotInputWhenDisabled { get; set; }

    public void SetWindowDecorations(WindowDecorations enabled) { }

    public void SetIcon(IWindowIconImpl? icon) { }

    public void ShowTaskbarIcon(bool value) { }

    public void CanResize(bool value) { }

    public void SetCanMinimize(bool value) { }

    public void SetCanMaximize(bool value) { }

    public Func<WindowCloseReason, bool>? Closing { get; set; }

    public bool IsClientAreaExtendedToDecorations => false;

    public Action<bool>? ExtendClientAreaToDecorationsChanged { get; set; }

    public bool NeedsManagedDecorations => false;

    public PlatformRequestedDrawnDecoration RequestedDrawnDecorations =>
        PlatformRequestedDrawnDecoration.None;

    public Action? DrawnDecorationsRequestChanged { get; set; }

    public Thickness ExtendedMargins => default;

    public Thickness OffScreenMargin => default;

    public void BeginMoveDrag(PointerPressedEventArgs e) { }

    public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e) { }

    public void Move(PixelPoint point) { }

    public void SetMinMaxSize(Size minSize, Size maxSize) { }

    public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint) { }

    public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight) { }

    void IWindowImpl.SetShadowExtents(Thickness extents) { }

    // ---- sizing ---------------------------------------------------------

    public void Resize(Size clientSize, WindowResizeReason reason = WindowResizeReason.Application)
    {
        if (_resizeDepth > 5)
            return;

        var width = Math.Max(1, clientSize.Width);
        var height = Math.Max(1, clientSize.Height);
        var target = new Size(width, height);

        if (Math.Abs(target.Width - ClientSize.Width) < 0.01 &&
            Math.Abs(target.Height - ClientSize.Height) < 0.01)
            return;

        try
        {
            _resizeDepth++;
            _requestedSize = target;

            // Post the new size before waking the render loop so the commit
            // that attaches the next buffer carries the new surface size.
            _surfaceProxy?.SetSize(
                (uint)Math.Max(1, Math.Round(width, MidpointRounding.AwayFromZero)),
                (uint)Math.Max(1, Math.Round(height, MidpointRounding.AwayFromZero)));

            ClientSize = target;
            Resized?.Invoke(target, reason);
        }
        finally
        {
            _resizeDepth--;
        }
    }

    public override object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(ITextInputMethodImpl))
            return null;

        if (featureType == typeof(IStorageProvider))
            return _storageProvider ??= new FallbackStorageProvider(BuildStorageFactories());

        return base.TryGetFeature(featureType);
    }

    private Func<System.Threading.Tasks.Task<IStorageProvider?>>[] BuildStorageFactories() =>
    [
        // A layer surface has no xdg_toplevel to export via xdg-foreign, so
        // there is no valid parent_window handle for the portal dialog.
        // Use the managed in-process picker instead of an unparented portal
        // window.
        () => System.Threading.Tasks.Task.FromResult<IStorageProvider?>(
            ((PresentationSource?)InputRoot)?.FocusRoot is TopLevel topLevel
                ? new ManagedStorageProvider(topLevel)
                : null),
    ];

    private new sealed class Sink : WindowBaseImpl.Sink, IWLayerSurfaceEventSink
    {
        private new LayerShellWindowImpl Parent => (LayerShellWindowImpl)base.Parent;
        private LayerSurfaceCreateResult? _handle;
        private WLayerSurfaceProxy? _surfaceProxy;
        private LayerConfigure? _initialConfigure;

        public Sink(LayerShellWindowImpl parent) : base(parent)
        {
            _handle = Parent.Client.CreateLayerSurfaceHandle(
                new WLayerSurfaceEventSinkProxy(this, WaylandMarshallers.UIThread),
                Parent._options with
                {
                    Width = Parent._requestedSize.Width,
                    Height = Parent._requestedSize.Height,
                });

            _surfaceProxy = _handle.Proxy;
            Parent._handle = _handle;
            Parent._surfaceProxy = _surfaceProxy;

            var initial = _handle.BasicInitCompleted.GetAwaiter().GetResult();
            _initialConfigure = initial;
            Parent.ApplyConfigure(initial);
            _surfaceProxy.SetPendingAckSerial(initial.Serial);

            if (Parent.CurrentCursor is not null)
                Parent.ApplyCurrentCursor(_surfaceProxy);
        }

        protected override void DisconnectFromSurface()
        {
            var proxy = Parent._surfaceProxy;
            Parent._handle = null;
            Parent._surfaceProxy = null;
            proxy?.Disconnect();
        }

        public void OnConfigure(LayerConfigure configure)
        {
            if (IsDisposed)
                return;

            // The constructor already applied the initial configure
            // synchronously; skip its marshalled duplicate.
            if (ReferenceEquals(configure, _initialConfigure))
            {
                _initialConfigure = null;
                return;
            }

            Parent.ApplyConfigure(configure);
            _surfaceProxy?.SetPendingAckSerial(configure.Serial);
        }

        public void OnClosed()
        {
            if (IsDisposed)
                return;

            if (Parent.Closing?.Invoke(WindowCloseReason.WindowClosing) != true)
                Dispose();
        }

        protected override void OnInputWhileDisabled() => Parent.GotInputWhenDisabled?.Invoke();
    }
}
