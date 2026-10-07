using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using NWayland;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>xdg_surface: window geometry + the configure/ack serial handshake. Backs either an
/// <see cref="XdgToplevelState"/> or an <see cref="XdgPopupState"/> role (both render as their own host).</summary>
internal sealed class XdgSurfaceState
{
    public XdgSurfaceState(XdgSurface.Server resource, SurfaceState surface)
    {
        Resource = resource;
        Surface = surface;
        surface.XdgSurface = this;
    }

    public XdgSurface.Server Resource { get; }
    public SurfaceState Surface { get; }
    public XdgToplevelState? Toplevel { get; set; }
    public XdgPopupState? Popup { get; set; }

    /// <summary>True once this xdg_surface carries a mapped render root (toplevel or popup) the UI hosts.</summary>
    public bool IsMappedRenderRoot => (Toplevel?.IsMapped ?? false) || (Popup?.IsMapped ?? false);
    /// <summary>The host id the role behind this xdg_surface renders under (0 until mapped).</summary>
    public uint RenderHostId => Toplevel?.HostId ?? Popup?.HostId ?? 0;

    public int GeometryX { get; private set; }
    public int GeometryY { get; private set; }
    public int GeometryWidth { get; private set; }
    public int GeometryHeight { get; private set; }
    public bool HasGeometry { get; private set; }

    private uint _lastConfigureSerial;
    private bool _configureAcked;
    private bool _initialConfigureSent;

    public void SetWindowGeometry(int x, int y, int width, int height)
    {
        GeometryX = x;
        GeometryY = y;
        GeometryWidth = width;
        GeometryHeight = height;
        HasGeometry = true;
    }

    public void AckConfigure(uint serial)
    {
        if (serial == _lastConfigureSerial)
            _configureAcked = true;
    }

    public uint SendConfigure(CompositorState state)
    {
        var serial = state.NextSerial();
        _lastConfigureSerial = serial;
        Resource.Configure(serial);
        return serial;
    }

    /// <summary>Initial toplevel configure on role assignment: 0x0 means the client picks its own size.</summary>
    public void SendInitialConfigure(CompositorState state)
    {
        if (_initialConfigureSent)
            return;
        _initialConfigureSent = true;
        Toplevel?.SendConfigure(0, 0, ReadOnlySpan<byte>.Empty);
        SendConfigure(state);
    }

    public void OnSurfaceCommit(CompositorState state)
    {
        if (!_configureAcked)
            return;
        if (Toplevel is { IsMapped: false } toplevel)
            toplevel.Map(state);
        else if (Popup is { IsMapped: false } popup)
            popup.Map(state);
    }

    public void Destroy(CompositorState state)
    {
        Surface.XdgSurface = null;
        Toplevel?.Unmap(state);
        Toplevel = null;
        Popup?.Unmap(state);
        Popup = null;
        state.UnregisterXdgSurface(Resource);
    }
}

/// <summary>xdg_toplevel: title/app_id and the host-window (auto-window, scenario 2) lifecycle.</summary>
internal sealed class XdgToplevelState
{
    public XdgToplevelState(XdgToplevel.Server resource, XdgSurfaceState xdgSurface)
    {
        Resource = resource;
        XdgSurface = xdgSurface;
        xdgSurface.Toplevel = this;
    }

    public XdgToplevel.Server Resource { get; }
    public XdgSurfaceState XdgSurface { get; }

    public string Title { get; set; } = "";
    public string AppId { get; set; } = "";
    public bool IsMapped { get; private set; }
    public uint HostId { get; private set; }

    // xdg_toplevel.set_min_size / set_max_size (0 ⇒ that dimension is unconstrained).
    public int MinWidth { get; private set; }
    public int MinHeight { get; private set; }
    public int MaxWidth { get; private set; }
    public int MaxHeight { get; private set; }

    public void SetMinSize(int width, int height) { MinWidth = width; MinHeight = height; }
    public void SetMaxSize(int width, int height) { MaxWidth = width; MaxHeight = height; }

    private uint? _embeddedHostId;
    /// <summary>True once an embedding host id has been assigned (scenario 1).</summary>
    public bool IsEmbedded => _embeddedHostId is not null;
    /// <summary>Scenario 1: bind this toplevel to a pre-registered host id (set by embed_toplevel before map),
    /// so it renders into an existing Avalonia control instead of an auto-hosted window.</summary>
    public void SetEmbeddedHost(uint hostId) => _embeddedHostId = hostId;

    // Scenario 5 (mark_content_surface): the content cookie the toolkit tagged THIS surface with, if any. Like
    // _embeddedHostId it can be set before the toplevel maps (the host id only exists at map), so Map() emits the
    // ContentSurfaceMarkedEvent once HostId is assigned. Durable (not cleared on unmap); a cookie is a fresh Guid
    // so a stale value here is harmless — it's gated on the cookie still being registered when Map runs.
    private string? _contentCookie;
    /// <summary>Record that the toolkit marked this surface as an Avalonia-content container with <paramref name="cookie"/>.</summary>
    public void SetContentCookie(string cookie) => _contentCookie = cookie;

    /// <summary>xdg-foreign (scenario 3/4): the imported parent toplevel this one was made a child of (via
    /// zxdg_imported_v2.set_parent_of). Read at <see cref="Map"/> to own the auto-window to its parent.</summary>
    public XdgToplevelState? ForeignParent { get; private set; }
    public void SetForeignParent(XdgToplevelState? parent) => ForeignParent = parent;

    /// <summary>xdg-foreign (scenario 3, OUT): the host-side handle of an exported Avalonia Window this toplevel was
    /// made a child of (zxdg_imported_v2.set_parent_of against a window handle). Read at <see cref="Map"/> to own the
    /// auto-window to that exported window. Recorded before map (the at-creation owner path real toolkits use).</summary>
    public string? ForeignParentWindowHandle { get; private set; }
    public void SetForeignParentWindow(string handle) => ForeignParentWindowHandle = handle;

    // Foreign handles this toplevel was exported under (zxdg_exporter_v2). Tracked so Destroy can revoke them —
    // otherwise a handle outlives the toplevel and a later import resolves a dead object (a stale-entry leak).
    private List<string>? _exportHandles;
    public void AddExportHandle(string handle) => (_exportHandles ??= new List<string>()).Add(handle);
    public void RemoveExportHandle(string handle) => _exportHandles?.Remove(handle);

    public void SendConfigure(int width, int height, ReadOnlySpan<byte> states) => Resource.Configure(width, height, states);
    public void SendClose() => Resource.Close();

    /// <summary>True while this toplevel holds keyboard focus; mirrored into the xdg_toplevel.configure
    /// <c>activated</c> state so the client styles itself as the active window.</summary>
    public bool Activated { get; private set; }

    // Last size we configured the client at, re-sent on a state-only re-configure (e.g. a focus change) so the
    // activated flag doesn't disturb the window size. 0 ⇒ "client keeps its own size" (the xdg-shell default).
    private int _lastConfiguredWidth;
    private int _lastConfiguredHeight;

    /// <summary>Host-driven resize: ask the client to re-lay-out to (width,height) via the configure handshake,
    /// carrying the current toplevel state (e.g. <c>activated</c>).</summary>
    public void SendResizeConfigure(CompositorState state, int width, int height)
        => SendStateConfigure(state, width, height);

    /// <summary>Set keyboard-focus state; on a change, re-configure the (mapped) client so it learns its new
    /// activated state, preserving the last configured size.</summary>
    public void SetActivated(CompositorState state, bool activated)
    {
        if (Activated == activated)
            return;
        Activated = activated;
        if (IsMapped)
            SendStateConfigure(state, _lastConfiguredWidth, _lastConfiguredHeight);
    }

    private void SendStateConfigure(CompositorState state, int width, int height)
    {
        _lastConfiguredWidth = width;
        _lastConfiguredHeight = height;
        // xdg_toplevel.configure `states` is a wl_array of little-endian uint32 enum values.
        Span<byte> states = stackalloc byte[16];
        var count = 0;
        if (Activated)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(states[count..], (uint)XdgToplevel.StateEnum.Activated);
            count += sizeof(uint);
        }
        SendConfigure(width, height, states[..count]);
        XdgSurface.SendConfigure(state);
    }

    public void Map(CompositorState state)
    {
        if (IsMapped)
            return;
        IsMapped = true;
        HostId = _embeddedHostId ?? state.NextHostId();
        state.RegisterToplevelHost(HostId, this);
        var width = XdgSurface.HasGeometry ? XdgSurface.GeometryWidth : XdgSurface.Surface.Width;
        var height = XdgSurface.HasGeometry ? XdgSurface.GeometryHeight : XdgSurface.Surface.Height;
        // Seed the configure size so a focus-driven activated re-configure preserves the client's current size
        // rather than collapsing to 0×0 (a host-driven resize overwrites these via SendResizeConfigure).
        _lastConfiguredWidth = width;
        _lastConfiguredHeight = height;
        // ForeignParent is read now (not at set_parent_of time) so the parent's host id is live by the time the
        // UI manufactures the auto-window — real toolkits set_parent_of before mapping the child.
        var parentHostId = ForeignParent is { IsMapped: true } p ? p.HostId : 0;
        // The surface's wayland object id (same value the client sees via wl_proxy_get_id) plus this connection's
        // ticket let the UI match a toolkit window back to its host with no client round-trip — ids are only unique
        // per connection, so the ticket scopes them. Used to target only resized widgets.
        state.ToUi.Enqueue(new ToplevelMappedEvent(
            HostId, Title, AppId, MinWidth, MinHeight, MaxWidth, MaxHeight, width, height, parentHostId,
            ForeignParentWindowHandle, XdgSurface.Surface.Resource.ObjectId, XdgSurface.Surface.Client.Ticket));
        // Scenario 5 (deferred resolve): if the toolkit marked this surface as a content container before it had
        // mapped, the host id only exists now — emit the binding AFTER ToplevelMappedEvent (so the UI has created
        // and registered the host control by the time it drains this) for the UI to overlay its content host.
        if (_contentCookie is { } cookie && state.IsContentCookieRegistered(cookie))
            state.ToUi.Enqueue(new ContentSurfaceMarkedEvent(cookie, HostId));
    }

    public void Unmap(CompositorState state)
    {
        if (!IsMapped)
            return;
        IsMapped = false;
        state.UnregisterToplevelHost(HostId);
        state.ToUi.Enqueue(new ToplevelUnmappedEvent(HostId));
    }

    public void Destroy(CompositorState state)
    {
        XdgSurface.Toplevel = null;
        if (_exportHandles is not null)
        {
            // Revoke any still-published foreign handles so a later import of them resolves to nothing (inert)
            // rather than to this destroyed toplevel.
            foreach (var handle in _exportHandles)
                state.RevokeForeignHandle(handle);
            _exportHandles.Clear();
        }
        Unmap(state);
    }
}

/// <summary>
/// xdg_popup: a child surface positioned relative to a parent xdg_surface via an xdg_positioner snapshot. Each
/// popup renders into its own Avalonia <c>Popup</c> (light-dismiss = the xdg <c>grab</c>), keyed by its own host
/// id, and a stack of popups is dismissed newest-first (<see cref="SendPopupDone"/> walks children in reverse).
/// </summary>
internal sealed class XdgPopupState
{
    private readonly List<XdgPopupState> _children = new();
    private bool _initialConfigureSent;
    private bool _doneSent;
    private PositionerSnapshot _positioner;

    public XdgPopupState(XdgPopup.Server resource, XdgSurfaceState xdgSurface, XdgSurfaceState parent, PositionerSnapshot positioner)
    {
        Resource = resource;
        XdgSurface = xdgSurface;
        Parent = parent;
        _positioner = positioner;
        xdgSurface.Popup = this;
        parent.Popup?.AddChild(this);
    }

    public XdgPopup.Server Resource { get; }
    public XdgSurfaceState XdgSurface { get; }
    public XdgSurfaceState Parent { get; }
    public bool IsMapped { get; private set; }
    public uint HostId { get; private set; }
    public bool IsGrab { get; private set; }

    public void SetGrab() => IsGrab = true;
    private void AddChild(XdgPopupState child) => _children.Add(child);
    private void RemoveChild(XdgPopupState child) => _children.Remove(child);

    /// <summary>Send the popup's positioned configure (geometry then the xdg_surface serial) once, on role setup.</summary>
    public void SendInitialConfigure(CompositorState state)
    {
        if (_initialConfigureSent)
            return;
        _initialConfigureSent = true;
        SendPositionedConfigure(state);
    }

    private void SendPositionedConfigure(CompositorState state)
    {
        var (x, y, w, h) = _positioner.Compute();
        Resource.Configure(x, y, w, h);    // xdg_popup.configure (relative to the parent window geometry)
        XdgSurface.SendConfigure(state);   // xdg_surface.configure(serial) — the client acks this
    }

    /// <summary>xdg_popup.reposition: snapshot the new positioner, announce the token, re-configure, and (if
    /// mapped) tell the UI to re-place the live Avalonia popup so it doesn't stay at the old geometry.</summary>
    public void Reposition(CompositorState state, PositionerSnapshot positioner, uint token)
    {
        _positioner = positioner;
        Resource.Repositioned(token);      // must precede the configure pair (xdg-shell ordering)
        SendPositionedConfigure(state);
        if (IsMapped)
        {
            var (x, y, w, h) = _positioner.Compute();
            state.ToUi.Enqueue(new PopupRepositionedEvent(HostId, x, y, w, h, _positioner));
        }
    }

    public void Map(CompositorState state)
    {
        if (IsMapped)
            return;
        IsMapped = true;
        HostId = state.NextHostId();
        state.RegisterPopupHost(HostId, this);
        var (x, y, w, h) = _positioner.Compute();
        state.ToUi.Enqueue(new PopupMappedEvent(HostId, Parent.RenderHostId, x, y, w, h, IsGrab, _positioner));
    }

    /// <summary>Tell the client to dismiss this popup (and its child stack, newest-first); it responds by
    /// destroying the popup. Used for light-dismiss / when the parent goes away.</summary>
    public void SendPopupDone()
    {
        for (var i = _children.Count - 1; i >= 0; i--)
            _children[i].SendPopupDone();
        if (_doneSent)
            return;
        _doneSent = true;
        Resource.PopupDone();
    }

    public void Unmap(CompositorState state)
    {
        // Children must go away before us; ask any still-open ones to dismiss (defensive — a well-behaved client
        // destroys popups newest-first, so they're usually already gone).
        for (var i = _children.Count - 1; i >= 0; i--)
            _children[i].SendPopupDone();
        _children.Clear(); // don't pin dismissed/destroyed child popups (they also detach themselves on their unmap)
        Parent.Popup?.RemoveChild(this);
        if (!IsMapped)
            return;
        IsMapped = false;
        state.UnregisterPopupHost(HostId);
        state.ToUi.Enqueue(new PopupUnmappedEvent(HostId));
    }

    public void Destroy(CompositorState state)
    {
        XdgSurface.Popup = null;
        Unmap(state);
    }
}

/// <summary>Immutable copy of an xdg_positioner's parameters, taken at get_popup time, plus the placement math.</summary>
internal readonly record struct PositionerSnapshot(
    int Width, int Height, int RectX, int RectY, int RectWidth, int RectHeight,
    XdgPositioner.AnchorEnum Anchor, XdgPositioner.GravityEnum Gravity,
    XdgPositioner.ConstraintAdjustmentEnum ConstraintAdjustment, int OffsetX, int OffsetY)
{
    /// <summary>The popup's geometry relative to the parent's window-geometry origin: anchor point on the anchor
    /// rect, shifted by gravity and offset. Pre-constraint (the UI's Avalonia Popup does the on-screen flip/slide).</summary>
    public (int X, int Y, int Width, int Height) Compute()
    {
        var w = Width > 0 ? Width : 1;
        var h = Height > 0 ? Height : 1;
        var ax = RectX + Anchor switch
        {
            XdgPositioner.AnchorEnum.Left or XdgPositioner.AnchorEnum.TopLeft or XdgPositioner.AnchorEnum.BottomLeft => 0,
            XdgPositioner.AnchorEnum.Right or XdgPositioner.AnchorEnum.TopRight or XdgPositioner.AnchorEnum.BottomRight => RectWidth,
            _ => RectWidth / 2,
        };
        var ay = RectY + Anchor switch
        {
            XdgPositioner.AnchorEnum.Top or XdgPositioner.AnchorEnum.TopLeft or XdgPositioner.AnchorEnum.TopRight => 0,
            XdgPositioner.AnchorEnum.Bottom or XdgPositioner.AnchorEnum.BottomLeft or XdgPositioner.AnchorEnum.BottomRight => RectHeight,
            _ => RectHeight / 2,
        };
        var x = ax + OffsetX + Gravity switch
        {
            XdgPositioner.GravityEnum.Left or XdgPositioner.GravityEnum.TopLeft or XdgPositioner.GravityEnum.BottomLeft => -w,
            XdgPositioner.GravityEnum.Right or XdgPositioner.GravityEnum.TopRight or XdgPositioner.GravityEnum.BottomRight => 0,
            _ => -w / 2,
        };
        var y = ay + OffsetY + Gravity switch
        {
            XdgPositioner.GravityEnum.Top or XdgPositioner.GravityEnum.TopLeft or XdgPositioner.GravityEnum.TopRight => -h,
            XdgPositioner.GravityEnum.Bottom or XdgPositioner.GravityEnum.BottomLeft or XdgPositioner.GravityEnum.BottomRight => 0,
            _ => -h / 2,
        };
        return (x, y, w, h);
    }
}

internal sealed class XdgWmBaseListener : XdgWmBase.ServerListener
{
    private readonly ClientContext _client;
    public XdgWmBaseListener(ClientContext client) => _client = client;

    protected override void GetXdgSurface(XdgWmBase.Server resource,
        NewId<XdgSurface.Server, XdgSurface.ServerListener> id, WlSurface.Server? surface)
    {
        var surfaceState = _client.State.GetSurface(surface);
        var listener = new XdgSurfaceListener(_client);
        var xdgSurfaceResource = id.GetAndConsume(listener);
        if (surfaceState is null)
            return;
        var state = new XdgSurfaceState(xdgSurfaceResource, surfaceState);
        _client.State.RegisterXdgSurface(xdgSurfaceResource, state);
        listener.Init(state);
    }

    protected override void CreatePositioner(XdgWmBase.Server resource, NewId<XdgPositioner.Server, XdgPositioner.ServerListener> id)
    {
        var listener = new XdgPositionerListener(_client);
        var positioner = id.GetAndConsume(listener);
        _client.State.RegisterPositioner(positioner, listener);
    }

    protected override void Pong(XdgWmBase.Server resource, uint serial) { }
    protected override void Destroy(XdgWmBase.Server resource) => resource.Dispose();
}

internal sealed class XdgSurfaceListener : XdgSurface.ServerListener
{
    private readonly ClientContext _client;
    private XdgSurfaceState? _state;

    public XdgSurfaceListener(ClientContext client) => _client = client;
    public void Init(XdgSurfaceState state) => _state = state;

    protected override void GetToplevel(XdgSurface.Server resource, NewId<XdgToplevel.Server, XdgToplevel.ServerListener> id)
    {
        var listener = new XdgToplevelListener(_client);
        var toplevelResource = id.GetAndConsume(listener);
        // An xdg_surface may be given only one role; a second role request is a protocol violation. Don't
        // overwrite the existing role (which would desync the compositor) — leave the new object inert.
        if (_state is null || _state.Toplevel is not null || _state.Popup is not null)
            return;
        var toplevel = new XdgToplevelState(toplevelResource, _state);
        listener.Init(toplevel);
        _state.SendInitialConfigure(_client.State);
    }

    protected override void GetPopup(XdgSurface.Server resource, NewId<XdgPopup.Server, XdgPopup.ServerListener> id,
        XdgSurface.Server? parent, XdgPositioner.Server? positioner)
    {
        var listener = new XdgPopupListener(_client);
        var popupResource = id.GetAndConsume(listener);
        var parentState = _client.State.GetXdgSurface(parent);
        var positionerSnapshot = _client.State.GetPositioner(positioner)?.Snapshot() ?? default;
        // One role per xdg_surface (see GetToplevel); leave a second-role popup inert rather than desync state.
        if (_state is null || parentState is null || _state.Toplevel is not null || _state.Popup is not null)
            return;
        var popup = new XdgPopupState(popupResource, _state, parentState, positionerSnapshot);
        listener.Init(popup);
        popup.SendInitialConfigure(_client.State);
    }

    protected override void SetWindowGeometry(XdgSurface.Server resource, int x, int y, int width, int height)
        => _state?.SetWindowGeometry(x, y, width, height);

    protected override void AckConfigure(XdgSurface.Server resource, uint serial) => _state?.AckConfigure(serial);

    protected override void Destroy(XdgSurface.Server resource)
    {
        _state?.Destroy(_client.State);
        resource.Dispose();
    }
}

internal sealed class XdgToplevelListener : XdgToplevel.ServerListener
{
    private readonly ClientContext _client;
    private XdgToplevelState? _state;

    public XdgToplevelListener(ClientContext client) => _client = client;
    public void Init(XdgToplevelState state) => _state = state;

    protected override void SetTitle(XdgToplevel.Server resource, string title)
    {
        if (_state is not null)
            _state.Title = title;
    }

    protected override void SetAppId(XdgToplevel.Server resource, string appId)
    {
        if (_state is not null)
            _state.AppId = appId;
    }

    protected override void SetParent(XdgToplevel.Server resource, XdgToplevel.Server? parent) { }
    protected override void ShowWindowMenu(XdgToplevel.Server resource, WlSeat.Server? seat, uint serial, int x, int y) { }
    protected override void Move(XdgToplevel.Server resource, WlSeat.Server? seat, uint serial) { }
    protected override void Resize(XdgToplevel.Server resource, WlSeat.Server? seat, uint serial, XdgToplevel.ResizeEdgeEnum edges) { }
    protected override void SetMaxSize(XdgToplevel.Server resource, int width, int height) => _state?.SetMaxSize(width, height);
    protected override void SetMinSize(XdgToplevel.Server resource, int width, int height) => _state?.SetMinSize(width, height);
    protected override void SetMaximized(XdgToplevel.Server resource) { }
    protected override void UnsetMaximized(XdgToplevel.Server resource) { }
    protected override void SetFullscreen(XdgToplevel.Server resource, WlOutput.Server? output) { }
    protected override void UnsetFullscreen(XdgToplevel.Server resource) { }
    protected override void SetMinimized(XdgToplevel.Server resource) { }

    protected override void Destroy(XdgToplevel.Server resource)
    {
        _state?.Destroy(_client.State);
        resource.Dispose();
    }
}

internal sealed class XdgPositionerListener : XdgPositioner.ServerListener
{
    private readonly ClientContext _client;
    public XdgPositionerListener(ClientContext client) => _client = client;

    private int _width, _height;
    private int _rectX, _rectY, _rectWidth, _rectHeight;
    private XdgPositioner.AnchorEnum _anchor;
    private XdgPositioner.GravityEnum _gravity;
    private XdgPositioner.ConstraintAdjustmentEnum _constraint;
    private int _offsetX, _offsetY;

    /// <summary>Freeze the current parameters into the immutable snapshot a popup keeps (xdg requires the
    /// positioner be fully set before get_popup; later mutations don't affect already-created popups).</summary>
    public PositionerSnapshot Snapshot() => new(
        _width, _height, _rectX, _rectY, _rectWidth, _rectHeight, _anchor, _gravity, _constraint, _offsetX, _offsetY);

    protected override void SetSize(XdgPositioner.Server resource, int width, int height) { _width = width; _height = height; }
    protected override void SetAnchorRect(XdgPositioner.Server resource, int x, int y, int width, int height) { _rectX = x; _rectY = y; _rectWidth = width; _rectHeight = height; }
    protected override void SetAnchor(XdgPositioner.Server resource, XdgPositioner.AnchorEnum anchor) => _anchor = anchor;
    protected override void SetGravity(XdgPositioner.Server resource, XdgPositioner.GravityEnum gravity) => _gravity = gravity;
    protected override void SetConstraintAdjustment(XdgPositioner.Server resource, XdgPositioner.ConstraintAdjustmentEnum constraintAdjustment) => _constraint = constraintAdjustment;
    protected override void SetOffset(XdgPositioner.Server resource, int x, int y) { _offsetX = x; _offsetY = y; }
    protected override void SetReactive(XdgPositioner.Server resource) { }
    protected override void SetParentSize(XdgPositioner.Server resource, int parentWidth, int parentHeight) { }
    protected override void SetParentConfigure(XdgPositioner.Server resource, uint serial) { }

    protected override void Destroy(XdgPositioner.Server resource)
    {
        _client.State.UnregisterPositioner(resource);
        resource.Dispose();
    }
}

internal sealed class XdgPopupListener : XdgPopup.ServerListener
{
    private readonly ClientContext _client;
    private XdgPopupState? _state;

    public XdgPopupListener(ClientContext client) => _client = client;
    public void Init(XdgPopupState state) => _state = state;

    protected override void Grab(XdgPopup.Server resource, WlSeat.Server? seat, uint serial) => _state?.SetGrab();

    protected override void Reposition(XdgPopup.Server resource, XdgPositioner.Server? positioner, uint token)
    {
        if (_state is null)
            return;
        var snapshot = _client.State.GetPositioner(positioner)?.Snapshot() ?? default;
        _state.Reposition(_client.State, snapshot, token);
    }

    protected override void Destroy(XdgPopup.Server resource)
    {
        _state?.Destroy(_client.State);
        resource.Dispose();
    }
}
