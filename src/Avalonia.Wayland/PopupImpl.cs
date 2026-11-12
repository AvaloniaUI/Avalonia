using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland;

/// <summary>
/// Wayland xdg_popup window implementation. Created via
/// <see cref="WindowImpl.CreatePopup"/> (or another <see cref="PopupImpl.CreatePopup"/>
/// for nested popups) and parented to either a top-level or another popup.
///
/// <para>We deliberately do NOT call <c>xdg_popup.grab()</c>. Dismissal is
/// driven by: (1) framework light-dismiss, (2) the compositor's
/// <c>popup_done</c> event, (3) focus-leave on the parent toplevel.</para>
///
/// <para>Coordinate convention: the <see cref="PopupPositioner"/>'s
/// <see cref="IPopupPositioner.Update"/> receives anchor-rect coordinates
/// in the parent's client-area logical pixels (i.e. parent
/// buffer-relative). We forward them straight through to the worker via
/// <see cref="WaylandConversionExtensions.ToWayland(PopupPositionerParameters)"/>;
/// the worker handles the buffer→geometry origin shift and clamping.</para>
/// </summary>
internal partial class PopupImpl : WindowBaseImpl, IPopupImpl
{
    private readonly WindowBaseImpl _parent;
    private readonly WaylandWorkerClient _workerClient;
    private WaylandSurfaceCreateResult<WXdgPopupProxy>? _handle;
    private WXdgPopupProxy? _surfaceProxy;
    private XdgPopupPositionerParams? _lastPositioner;
    private bool _isHitTestVisible = true;
    private PixelRect? _configuredGeometry;
    private Thickness _shadowExtents;
    private PixelPoint _position;

    public PopupImpl(WaylandWorkerClient client, WindowBaseImpl parent) : base(client)
    {
        _parent = parent;
        _workerClient = client;
        // Inherit the parent's render scale at construction time so the
        // first layout pass uses something sensible. The compositor will
        // re-issue scale events via OnScaleChanged once the popup is
        // mapped — at which point we'll converge on the authoritative value.
        RenderScaling = parent.RenderScaling;

        PopupPositioner = new WaylandPopupPositioner(this);
        _parent.ChildPositionsInvalidated += UpdatePosition;
    }

    public override void Dispose()
    {
        _parent.ChildPositionsInvalidated -= UpdatePosition;
        base.Dispose();
    }

    public override IPlatformRenderSurface[] Surfaces => _handle?.GetRenderSurfaces() ?? [];

    /// <summary>
    /// Inherit the parent's auto-size hint (which on Wayland comes from
    /// <c>xdg_toplevel.configure_bounds</c> — i.e. the usable area of
    /// the output the parent is on). xdg_popup is constrained by the
    /// compositor to fit on-screen, so the same bound applies.
    /// </summary>
    public override Size MaxAutoSizeHint => _parent.MaxAutoSizeHint;

    internal override WXdgShellSurfaceProxy? SurfaceProxy => _surfaceProxy;

    public override PixelPoint Position => _position;

    internal override PixelPoint WindowGeometryOrigin => new((int)_shadowExtents.Left, (int)_shadowExtents.Top);

    /// <summary>
    /// Derives the buffer-relative position from the last configure (parent
    /// window-geometry-relative) and both sides' geometry origins, chaining
    /// through the parent so nested popups end up in the toplevel's space.
    /// </summary>
    private void UpdatePosition()
    {
        if (_configuredGeometry is not { } g)
            return;
        var parentPos = _parent.Position;
        var parentOrigin = _parent.WindowGeometryOrigin;
        var ownOrigin = WindowGeometryOrigin;
        var position = new PixelPoint(
            parentPos.X + parentOrigin.X + g.X - ownOrigin.X,
            parentPos.Y + parentOrigin.Y + g.Y - ownOrigin.Y);
        if (_position == position)
            return;
        _position = position;
        PositionChanged?.Invoke(position);
        InvalidateChildPositions();
    }

    public IPopupPositioner PopupPositioner { get; }

    public override void Show(bool activate, bool isDialog)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(PopupImpl));

        if (CurrentSink == null)
            CurrentSink = new Sink(this);

        Client.AnyThreadWakeupRenderLoop();
    }

    public override IPopupImpl CreatePopup() => new PopupImpl(_workerClient, this);

    /// <summary>
    /// Avalonia drives popup positioning through this entry. We translate
    /// the framework's bitfield enums into the protocol's dense enums and
    /// hand the bundle to the worker, which (re-)builds an
    /// <c>xdg_positioner</c> on every connect.
    /// </summary>
    internal void UpdatePositioner(PopupPositionerParameters parameters)
    {
        var translated = parameters.ToWayland();
        _lastPositioner = translated;
        // Adopt the requested popup size as our client-size up-front so
        // the renderer paints at the right dimensions before the
        // compositor's first popup configure event arrives. ClientSize's
        // setter only stores + wakes the render loop, so fire Resized
        // explicitly to drive Avalonia's layout pass.
        if (translated.Size.Width > 0 && translated.Size.Height > 0
            && translated.Size != ClientSize)
        {
            ClientSize = translated.Size;
            Resized?.Invoke(translated.Size, WindowResizeReason.Layout);
        }
        // Mirrors the worker: a default Deflate leaves the previously set extents in place.
        if (translated.Deflate != default)
            _shadowExtents = translated.Deflate;
        _surfaceProxy?.UpdatePositioner(translated);
        UpdatePosition();
    }

    public void SetWindowManagerAddShadowHint(bool enabled)
    {
        // No-op on Wayland — the compositor doesn't draw popup shadows;
        // CSD shadows (if any) are baked into the buffer by the framework.
    }

    public void TakeFocus()
    {
    }

    public void SetHitTestVisible(bool isHitTestVisible)
    {
        _isHitTestVisible = isHitTestVisible;
        _surfaceProxy?.SetHitTestVisible(isHitTestVisible);
    }

    /// <summary>
    /// Bridges Avalonia's positioner contract to <see cref="UpdatePositioner"/>.
    /// </summary>
    private sealed class WaylandPopupPositioner(PopupImpl owner) : IPopupPositioner
    {
        public void Update(PopupPositionerParameters parameters) => owner.UpdatePositioner(parameters);
    }
}
