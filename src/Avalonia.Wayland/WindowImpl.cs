using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Dialogs;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland;

internal partial class WindowImpl : WindowBaseImpl, IWindowImpl
{
    private WaylandSurfaceCreateResult<WXdgTopLevelProxy>? _handle;
    private WXdgTopLevelProxy? _surfaceProxy;
    private WaylandTextInputMethod? _textInputMethod;
    private bool _isActivated;
    private Size _maxAutoSizeHint;
    private WindowState _windowState;
    private bool _canResize = true;
    private Size _restoreBounds;
    private Thickness _shadowExtents;
    private Size? _minSize;
    private Size? _maxSize;
    // Sticky-CSD latch on the UI side. Set true the first time the
    // framework asks for partial / no decorations. Never resets,
    // including across reconnects. 
    // This is a limitation of V1 of the protocol that's supported in the wild
    private bool _csdSticky;
    private string? _title;
    private FallbackStorageProvider? _storageProvider;

    public WindowImpl(WaylandWorkerClient client) : base(client)
    {
        _currentSink = new Sink(this, false);
    }    private void ApplyConfigureBatch(XdgConfigureBatch batch)
    {
        _maxAutoSizeHint = batch.MaxSize;

        // Map xdg_toplevel states to Avalonia WindowState
        var newWindowState = WindowState.Normal;
        if ((batch.States & XdgToplevelStates.Fullscreen) != 0)
            newWindowState = WindowState.FullScreen;
        else if ((batch.States & XdgToplevelStates.Maximized) != 0)
            newWindowState = WindowState.Maximized;

        var oldWindowState = _windowState;

        // Save restore bounds when leaving Normal state
        if (oldWindowState == WindowState.Normal && newWindowState != WindowState.Normal)
            _restoreBounds = _clientSize;

        if (oldWindowState != newWindowState)
        {
            _windowState = newWindowState;
            WindowStateChanged?.Invoke(newWindowState);
        }

        // Activation
        var isActivated = (batch.States & XdgToplevelStates.Activated) != 0;
        var wasActivated = _isActivated;
        _isActivated = isActivated;
        if (isActivated && !wasActivated)
            Activated?.Invoke();
        else if (!isActivated && wasActivated)
            Deactivated?.Invoke();

        // Resize: configure size is window geometry (excluding shadows).
        // Add shadow extents to get full surface size for the UI thread.
        if (batch.Size is { Width: > 0, Height: > 0 })
        {
            var newClientSize = new Size(
                batch.Size.Width + _shadowExtents.Left + _shadowExtents.Right,
                batch.Size.Height + _shadowExtents.Top + _shadowExtents.Bottom);
            if (newClientSize != _clientSize)
            {
                _clientSize = newClientSize;
                Resized?.Invoke(newClientSize, WindowResizeReason.Layout);
            }
        }
        else if (newWindowState == WindowState.Normal && oldWindowState != WindowState.Normal
                 && _restoreBounds is { Width: > 0, Height: > 0 })
        {
            if (_restoreBounds != _clientSize)
            {
                _clientSize = _restoreBounds;
                Resized?.Invoke(_restoreBounds, WindowResizeReason.Layout);
            }
        }
    }

    public override object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(ITextInputMethodImpl))
            return _textInputMethod ??= new WaylandTextInputMethod(this);
        if (featureType == typeof(IStorageProvider))
            return _storageProvider ??= new FallbackStorageProvider(BuildStorageFactories());
        return base.TryGetFeature(featureType);
    }

    private Func<Task<IStorageProvider?>>[] BuildStorageFactories() => new Func<Task<IStorageProvider?>>[]
    {
        // Probe xdg-foreign-v2 export at factory-eval time. The DBus portal is only
        // useful with a real wayland: parent_window — exposing the dialog as a
        // floating, unparented window is worse UX than a managed in-process picker.
        // If the probe fails (no exporter global, surface not mapped, or compositor
        // never delivers the handle), return null and let FallbackStorageProvider
        // move on to ManagedStorageProvider. The factory is re-evaluated on every
        // (re)connect via FallbackStorageProvider.Reset.
        async () =>
        {
            var firstLease = await AcquirePortalParentLeaseAsync();
            if (firstLease is null)
                return null;
            // We don't keep the probe lease around — each picker call exports fresh.
            await firstLease.DisposeAsync();
            return await DBusSystemDialog.TryCreateAsync(AcquirePortalParentLeaseAsync);
        },
        // HACK: relies on focus root being TopLevel which currently is true
        () => Task.FromResult(_inputRoot is TopLevel tl ? (IStorageProvider?)new ManagedStorageProvider(tl) : null)
    };

    private async Task<IPortalParentLease?> AcquirePortalParentLeaseAsync()
    {
        var proxy = _surfaceProxy;
        if (proxy is null)
            return null;
        // Cross-thread proxy auto-wraps the worker call into Task<IWaylandXdgTopLevelExport?>.
        var export = await proxy.ExportToplevel();
        if (export is null)
            return null;
        var raw = await export.HandleTask;
        if (raw is null)
        {
            export.Dispose();
            return null;
        }
        return new XdgForeignPortalParentLease(export, "wayland:" + raw);
    }

    private sealed class XdgForeignPortalParentLease : IPortalParentLease
    {
        private readonly IWaylandXdgTopLevelExport _export;
        public XdgForeignPortalParentLease(IWaylandXdgTopLevelExport export, string handle)
        {
            _export = export;
            Handle = handle;
        }
        public string Handle { get; }
        public ValueTask DisposeAsync()
        {
            _export.Dispose();
            return default;
        }
    }

    public override IPlatformRenderSurface[] Surfaces => _handle?.GetRenderSurfaces() ?? [];

    internal override WXdgShellSurfaceProxy? SurfaceProxy => _surfaceProxy;

    public override IPopupImpl? CreatePopup() => new PopupImpl(_client, this);

    // TODO: Query client decorations
    public override Size? FrameSize => null;

    public override void Show(bool activate, bool isDialog)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WindowImpl));

        if (_currentSink == null)
        {
            // TODO: Re-apply window state
            _currentSink = new Sink(this, true);
        }

        _client.AnyThreadWakeupRenderLoop();
    }

    public override Size MaxAutoSizeHint => _maxAutoSizeHint;

    public WindowState WindowState
    {
        get => _windowState;
        set
        {
            // Capture current state on the UI thread for the transition logic
            var currentState = _windowState;
            // Don't update the field — the compositor will confirm via configure
            var proxy = _surfaceProxy;
            if (proxy is null)
                return;
            switch (value)
            {
                case WindowState.Normal:
                    if (currentState == WindowState.Maximized)
                        proxy.UnsetMaximized();
                    else if (currentState == WindowState.FullScreen)
                        proxy.UnsetFullscreen();
                    break;
                case WindowState.Maximized:
                    if (currentState == WindowState.FullScreen)
                        proxy.UnsetFullscreen();
                    proxy.SetMaximized();
                    break;
                case WindowState.FullScreen:
                    if (currentState == WindowState.Maximized)
                        proxy.UnsetMaximized();
                    proxy.SetFullscreen();
                    break;
                case WindowState.Minimized:
                    proxy.SetMinimized();
                    break;
            }
        }
    }
    public bool WindowStateGetterIsUsable => true;
    public Action<WindowState>? WindowStateChanged { get; set; }
    public void SetTitle(string? title)
    {
        _title = title;
        _surfaceProxy?.SetTitle(title);
    }

    public void SetParent(IWindowImpl? parent)
    {
        var parentProxy = (parent as WindowImpl)?._handle?.Proxy;
        _surfaceProxy?.SetParent(parentProxy);
    }

    public void SetEnabled(bool enable) => _isEnabled = enable;

    public Action? GotInputWhenDisabled { get; set; }

    public void SetWindowDecorations(WindowDecorations enabled)
    {
        if (_csdSticky || enabled == WindowDecorations.Full)
            return;
        
        // We disable SSD support completely once CSD was requested at least once. Protocol limitation
        _csdSticky = true;
        _surfaceProxy?.DestroyDecoration();
        ApplyDecorationMode(DecorationMode.ClientSide);
    }

    /// <summary>
    /// Apply a new effective decoration mode on the UI thread. Updates
    /// the cached <see cref="NeedsManagedDecorations"/> flag and nudges the
    /// framework to re-evaluate chrome layout. Idempotent.
    /// </summary>
    internal void ApplyDecorationMode(DecorationMode mode)
    {
        if(_serverReportedMode == mode)
            return;
        _serverReportedMode = mode;
        RaiseDecorationChangeCallbacks();
    }

    private void RaiseDecorationChangeCallbacks()
    {
        DrawnDecorationsRequestChanged?.Invoke();
        ExtendClientAreaToDecorationsChanged?.Invoke(IsClientAreaExtendedToDecorations);
    }

    public void SetIcon(IWindowIconImpl? icon) { }
    public void ShowTaskbarIcon(bool value) { }
    public void CanResize(bool value) => _canResize = value;
    public void SetCanMinimize(bool value) { }
    public void SetCanMaximize(bool value) { }

    public Func<WindowCloseReason, bool>? Closing { get; set; }


    public Action<bool>? ExtendClientAreaToDecorationsChanged
    {
        get => field;
        set
        {
            field = value;
            value?.Invoke(true);
        }
    }

    private DecorationMode _serverReportedMode = DecorationMode.ClientSide;
    private bool _appRequestedDecorationExtension;

    public bool IsClientAreaExtendedToDecorations => NeedsManagedDecorations && _appRequestedDecorationExtension;
    public bool NeedsManagedDecorations => _serverReportedMode == DecorationMode.ClientSide;
    public PlatformRequestedDrawnDecoration RequestedDrawnDecorations =>
        NeedsManagedDecorations
            ? PlatformRequestedDrawnDecoration.TitleBar | PlatformRequestedDrawnDecoration.Border |
              PlatformRequestedDrawnDecoration.ResizeGrips | PlatformRequestedDrawnDecoration.Shadow
            : PlatformRequestedDrawnDecoration.None;

    public Action? DrawnDecorationsRequestChanged { get; set; }

    public Thickness ExtendedMargins => default;
    public Thickness OffScreenMargin => default;

    // Not supported by Wayland — surfaces have no screen position.
    public void Move(PixelPoint point) { }

    public void BeginMoveDrag(PointerPressedEventArgs e)
    {
        var cookie = e.PlatformInputEventCookie;
        e.Pointer.Capture(null);
        _surfaceProxy?.Move(cookie, WaylandDispatchPriority.Oob);
    }

    public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
    {
        var resizeEdge = edge switch
        {
            WindowEdge.NorthWest => XdgToplevel.ResizeEdgeEnum.TopLeft,
            WindowEdge.North => XdgToplevel.ResizeEdgeEnum.Top,
            WindowEdge.NorthEast => XdgToplevel.ResizeEdgeEnum.TopRight,
            WindowEdge.West => XdgToplevel.ResizeEdgeEnum.Left,
            WindowEdge.East => XdgToplevel.ResizeEdgeEnum.Right,
            WindowEdge.SouthWest => XdgToplevel.ResizeEdgeEnum.BottomLeft,
            WindowEdge.South => XdgToplevel.ResizeEdgeEnum.Bottom,
            WindowEdge.SouthEast => XdgToplevel.ResizeEdgeEnum.BottomRight,
            _ => XdgToplevel.ResizeEdgeEnum.None
        };
        var cookie = e.PlatformInputEventCookie;
        e.Pointer.Capture(null);
        _surfaceProxy?.Resize(cookie, resizeEdge, WaylandDispatchPriority.Oob);
    }

    private int _resizeStackDepth = 0;

    private bool SizeNearlyEquals(double x, double y) => Math.Abs(x - y) < 0.01;

    public void Resize(Size clientSize, WindowResizeReason reason = WindowResizeReason.Application)
    {
        // There is some bug on the x-plat side of things that increments the size by 0.000001 or smth and causes
        // a stack overflow
        // So we check for recursion depth and for nearly equal values here.
        
        if (_resizeStackDepth > 5) 
            return;

        if (SizeNearlyEquals(clientSize.Width, ClientSize.Width) &&
            SizeNearlyEquals(clientSize.Height, ClientSize.Height))
            return;
        try
        {
            _resizeStackDepth++;
            // Size is just a number, the actual resizing is done by the compositor when submitting a frame
            ClientSize = clientSize;
            Resized?.Invoke(clientSize, reason);
        }
        finally
        {
            _resizeStackDepth--;
        }
    }

    public void SetMinMaxSize(Size minSize, Size maxSize)
    {
        // Avalonia uses (0,0) and PositiveInfinity to mean "unconstrained".
        // Map both onto null at this boundary so the worker layer can
        // express "no constraint" without sentinel-value plumbing.
        static Size? Normalize(Size s) =>
            s.Width > 0 && s.Height > 0 && !double.IsPositiveInfinity(s.Width)
                && !double.IsPositiveInfinity(s.Height)
                ? s
                : (Size?)null;
        _minSize = Normalize(minSize);
        _maxSize = Normalize(maxSize);
        _surfaceProxy?.SetMinMaxSize(_minSize, _maxSize);
    }

    public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
    {
        _appRequestedDecorationExtension = extendIntoClientAreaHint;
        RaiseDecorationChangeCallbacks();
    }

    public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight) { }

    void IWindowImpl.SetShadowExtents(Thickness extents)
    {
        _shadowExtents = extents;
        _surfaceProxy?.SetShadowExtents(extents);
    }
}
