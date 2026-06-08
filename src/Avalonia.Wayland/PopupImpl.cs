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

    public PopupImpl(WaylandWorkerClient client, WindowBaseImpl parent) : base(client)
    {
        _parent = parent;
        _workerClient = client;
        // Inherit the parent's render scale at construction time so the
        // first layout pass uses something sensible. The compositor will
        // re-issue scale events via OnScaleChanged once the popup is
        // mapped — at which point we'll converge on the authoritative value.
        _renderScaling = parent.RenderScaling;

        PopupPositioner = new WaylandPopupPositioner(this);
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

    public IPopupPositioner PopupPositioner { get; }

    public override void Show(bool activate, bool isDialog)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PopupImpl));

        if (_currentSink == null)
            _currentSink = new Sink(this);

        _client.AnyThreadWakeupRenderLoop();
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
            && translated.Size != _clientSize)
        {
            ClientSize = translated.Size;
            Resized?.Invoke(translated.Size, WindowResizeReason.Layout);
        }
        _surfaceProxy?.UpdatePositioner(translated);
    }

    public void SetWindowManagerAddShadowHint(bool enabled)
    {
        // No-op on Wayland — the compositor doesn't draw popup shadows;
        // CSD shadows (if any) are baked into the buffer by the framework.
    }

    public void TakeFocus()
    {
    }

    /// <summary>
    /// Bridges Avalonia's positioner contract to <see cref="UpdatePositioner"/>.
    /// </summary>
    private sealed class WaylandPopupPositioner(PopupImpl owner) : IPopupPositioner
    {
        public void Update(PopupPositionerParameters parameters) => owner.UpdatePositioner(parameters);
    }
}
