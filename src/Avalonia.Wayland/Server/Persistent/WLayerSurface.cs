using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Transient;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.Wlr.WlrLayerShellUnstableV1;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// Worker-side <c>zwlr_layer_surface_v1</c>. A layer surface is a normal
/// <c>wl_surface</c> with a role that anchors it to an output edge and gives
/// the compositor a fixed logical size; it does not have an
/// <c>xdg_surface</c>, decorations, or a manually positioned geometry.
/// </summary>
/// <remarks>
/// The surface is created with an explicit size and the
/// <see cref="LayerShellAnchor.Top"/> anchor set. The compositor displays a
/// singleton top anchor centred horizontally on the output, which yields a
/// pill-sized top-centre surface without any screen-width arithmetic.
/// </remarks>
class WLayerSurface : WSurface, IWLayerSurface, IWXdgPopupParent
{
    private readonly WLayerSurfaceEventSinkProxy _eventSink;
    private readonly LayerShellOptions _options;

    private ZwlrLayerSurfaceV1? _layerSurface;
    private uint? _pendingAckSerial;
    private bool _initialConfigureAcknowledged;
    private bool _mapped;
    private readonly List<WXdgPopup> _pendingChildPopups = new();

    private uint _width;
    private uint _height;
    private ZwlrLayerSurfaceV1.AnchorEnum _anchor;
    private int _exclusiveZone;
    private Thickness _margin;
    private ZwlrLayerSurfaceV1.KeyboardInteractivityEnum _keyboardInteractivity;

    private readonly TaskCompletionSource<LayerConfigure> _basicInitCompleted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public WLayerSurface(
        WaylandWorker worker,
        WLayerSurfaceEventSinkProxy eventSink,
        LayerShellOptions options) : base(worker)
    {
        _eventSink = eventSink;
        _options = options;
        _width = ClampSize(options.Width);
        _height = ClampSize(options.Height);
        _anchor = ToWayland(options.Anchor);
        _exclusiveZone = options.ExclusiveZone;
        _margin = options.Margin;
        _keyboardInteractivity = ToWayland(options.KeyboardInteractivity);
    }

    internal override WSurfaceEventSinkProxy EventSink => _eventSink;

    public Task<LayerConfigure> BasicInitCompleted => _basicInitCompleted.Task;

    protected override bool CanCommitOutOfBand => _initialConfigureAcknowledged;

    public override PlatformRenderTargetState State =>
        _initialConfigureAcknowledged
            ? base.State
            : PlatformRenderTargetState.NotReadyWillWakeupRenderLoop;

    public override void OnConnected(WaylandConnection connection, WaylandGlobals globals)
    {
        base.OnConnected(connection, globals);

        if (globals.LayerShell is null)
            throw new AvaloniaWaylandException(
                "zwlr_layer_shell_v1 is not available on this compositor");

        _pendingAckSerial = null;
        _initialConfigureAcknowledged = false;
        _mapped = false;
        _pendingChildPopups.Clear();

        _layerSurface = globals.LayerShell.GetLayerSurface(
            WlSurface!,
            output: null,
            _options.Layer switch
            {
                LayerShellLayer.Background => ZwlrLayerShellV1.LayerEnum.Background,
                LayerShellLayer.Bottom => ZwlrLayerShellV1.LayerEnum.Bottom,
                LayerShellLayer.Top => ZwlrLayerShellV1.LayerEnum.Top,
                _ => ZwlrLayerShellV1.LayerEnum.Overlay,
            },
            _options.Namespace,
            new Listener(this),
            connection.Queue);

        ApplyProperties();

        // The protocol requires an initial commit without a buffer to make the
        // compositor emit the first configure event.
        WlSurface!.Commit();
    }

    private void ApplyProperties()
    {
        if (_layerSurface is null)
            return;

        _layerSurface.SetAnchor(_anchor);
        _layerSurface.SetSize(_width, _height);
        _layerSurface.SetExclusiveZone(_exclusiveZone);
        _layerSurface.SetMargin(
            (int)Math.Round(_margin.Top),
            (int)Math.Round(_margin.Right),
            (int)Math.Round(_margin.Bottom),
            (int)Math.Round(_margin.Left));
        _layerSurface.SetKeyboardInteractivity(_keyboardInteractivity);
    }

    public override void OnDisconnected()
    {
        _eventSink.OnKeyboardLeave();
        _layerSurface?.Destroy();
        _layerSurface = null;
        _pendingAckSerial = null;
        _initialConfigureAcknowledged = false;
        _mapped = false;
        _pendingChildPopups.Clear();
        _eventSink.OnSurfaceOutputsChanged(Array.Empty<object>());
        base.OnDisconnected();
    }

    /// <summary>
    /// Acks the pending configure and, on the first buffer, marks the surface
    /// mapped and drains child popups that were created before it could be
    /// used as a popup parent.
    /// </summary>
    public override void OnBeforeNewBufferAttached(IRenderTarget.RenderTargetSceneInfo sceneInfo)
    {
        if (_pendingAckSerial is { } serial)
        {
            _layerSurface!.AckConfigure(serial);
            _pendingAckSerial = null;
        }

        base.OnBeforeNewBufferAttached(sceneInfo);

        if (!_mapped)
        {
            _mapped = true;
            DrainPendingChildPopups();
        }
    }

    private void DrainPendingChildPopups()
    {
        if (_pendingChildPopups.Count == 0)
            return;

        var snapshot = _pendingChildPopups.ToArray();
        _pendingChildPopups.Clear();
        foreach (var popup in snapshot)
            popup.TryAttachToParent();
    }

    bool IWXdgPopupParent.IsMapped => _mapped;

    bool IWXdgPopupParent.IsPopupParentReady =>
        _mapped && _layerSurface != null && WlSurface != null;

    (int Left, int Top, int Width, int Height)? IWXdgPopupParent.PopupParentGeometry =>
        (0, 0, (int)_width, (int)_height);

    void IWXdgPopupParent.RegisterPendingChildPopup(WXdgPopup popup) =>
        _pendingChildPopups.Add(popup);

    void IWXdgPopupParent.UnregisterPendingChildPopup(WXdgPopup popup) =>
        _pendingChildPopups.Remove(popup);

    XdgPopup IWXdgPopupParent.CreatePopup(
        XdgSurface popupSurface,
        XdgPositioner positioner,
        XdgPopup.Listener listener)
    {
        if (_layerSurface is null)
            throw new AvaloniaWaylandException(
                "Cannot create a popup: the layer surface is not connected.");

        // A layer surface is not an xdg_surface, so the popup is first
        // created with a null parent and then associated with the layer
        // surface. Both calls must happen before the popup's first commit.
        var popup = popupSurface.GetPopup(null, positioner, listener);
        _layerSurface.GetPopup(popup);
        return popup;
    }

    public void SetSize(uint width, uint height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);

        if (_layerSurface is null)
            return;

        _layerSurface.SetSize(_width, _height);
        if (CanCommitOutOfBand)
            WlSurface!.Commit();
    }

    public void SetAnchor(ZwlrLayerSurfaceV1.AnchorEnum anchor)
    {
        _anchor = anchor;
        if (_layerSurface is null)
            return;

        _layerSurface.SetAnchor(anchor);
        if (CanCommitOutOfBand)
            WlSurface!.Commit();
    }

    public void SetMargin(int top, int right, int bottom, int left)
    {
        _margin = new Thickness(left, top, right, bottom);
        if (_layerSurface is null)
            return;

        _layerSurface.SetMargin(top, right, bottom, left);
        if (CanCommitOutOfBand)
            WlSurface!.Commit();
    }

    public void SetExclusiveZone(int zone)
    {
        _exclusiveZone = zone;
        if (_layerSurface is null)
            return;

        _layerSurface.SetExclusiveZone(zone);
        if (CanCommitOutOfBand)
            WlSurface!.Commit();
    }

    public void SetKeyboardInteractivity(ZwlrLayerSurfaceV1.KeyboardInteractivityEnum mode)
    {
        _keyboardInteractivity = mode;
        if (_layerSurface is null)
            return;

        _layerSurface.SetKeyboardInteractivity(mode);
        if (CanCommitOutOfBand)
            WlSurface!.Commit();
    }

    public void SetPendingAckSerial(uint serial)
    {
        _initialConfigureAcknowledged = true;
        _pendingAckSerial = serial;
        Worker.WakeupRenderLoop();
    }

    protected override void OnScaleChanged(double scale) => _eventSink.OnScaleChanged(scale);

    protected override void OnOutputsChanged()
    {
        base.OnOutputsChanged();

        var ids = Outputs.Count == 0
            ? (IReadOnlyList<object>)Array.Empty<object>()
            : Outputs.Select(o => o.Id).ToList();
        _eventSink.OnSurfaceOutputsChanged(ids);
    }

    private static uint ClampSize(double value) =>
        (uint)Math.Max(1, Math.Round(value, MidpointRounding.AwayFromZero));

    private static ZwlrLayerSurfaceV1.AnchorEnum ToWayland(LayerShellAnchor anchor)
    {
        var result = (ZwlrLayerSurfaceV1.AnchorEnum)0;
        if ((anchor & LayerShellAnchor.Top) != 0)
            result |= ZwlrLayerSurfaceV1.AnchorEnum.Top;
        if ((anchor & LayerShellAnchor.Bottom) != 0)
            result |= ZwlrLayerSurfaceV1.AnchorEnum.Bottom;
        if ((anchor & LayerShellAnchor.Left) != 0)
            result |= ZwlrLayerSurfaceV1.AnchorEnum.Left;
        if ((anchor & LayerShellAnchor.Right) != 0)
            result |= ZwlrLayerSurfaceV1.AnchorEnum.Right;
        return result;
    }

    private static ZwlrLayerSurfaceV1.KeyboardInteractivityEnum ToWayland(
        LayerShellKeyboardInteractivity mode) => mode switch
    {
        LayerShellKeyboardInteractivity.Exclusive =>
            ZwlrLayerSurfaceV1.KeyboardInteractivityEnum.Exclusive,
        LayerShellKeyboardInteractivity.OnDemand =>
            ZwlrLayerSurfaceV1.KeyboardInteractivityEnum.OnDemand,
        _ => ZwlrLayerSurfaceV1.KeyboardInteractivityEnum.None,
    };

    private sealed class Listener(WLayerSurface p) : ZwlrLayerSurfaceV1.Listener
    {
        protected override void Configure(
            ZwlrLayerSurfaceV1 sender, uint serial, uint width, uint height)
        {
            // A zero dimension means "the compositor chooses"; keep the size
            // we requested so the UI and the render target agree.
            var w = width == 0 ? p._width : width;
            var h = height == 0 ? p._height : height;
            p._width = w;
            p._height = h;

            var configure = new LayerConfigure(serial, (int)w, (int)h);
            p._basicInitCompleted.TrySetResult(configure);
            p._eventSink.OnConfigure(configure);
            p.Worker.WakeupRenderLoop();
        }

        protected override void Closed(ZwlrLayerSurfaceV1 sender) =>
            p._eventSink.OnClosed();
    }
}
