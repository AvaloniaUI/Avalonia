using System.Collections.Generic;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// Compositor-thread-owned registries and counters. Everything here is touched ONLY on the compositor
/// thread, so no locking is needed. Replaces the PoC's <c>Program.Compositor</c> statics: each listener is
/// handed the <see cref="ClientContext"/> it belongs to, which exposes this shared state, so surface/role
/// lookups never go through a global. Cross-thread references are always by the opaque ids minted here.
/// </summary>
internal sealed class CompositorState
{
    private readonly Dictionary<WlSurface.Server, SurfaceState> _surfacesByResource = new();
    private readonly Dictionary<uint, SurfaceState> _surfacesById = new();
    private readonly Dictionary<uint, XdgToplevelState> _toplevelsByHostId = new();
    private readonly Dictionary<uint, XdgPopupState> _popupsByHostId = new();
    private readonly Dictionary<XdgSurface.Server, XdgSurfaceState> _xdgSurfacesByResource = new();
    private readonly Dictionary<XdgPositioner.Server, XdgPositionerListener> _positionersByResource = new();
    private readonly Dictionary<string, XdgToplevelState> _foreignHandles = new();
    private readonly Dictionary<string, uint> _embedTokens = new();
    private readonly HashSet<string> _contentCookies = new();
    private uint _serial;
    private uint _surfaceId;
    private uint _hostId;
    private uint _foreignSeq;

    public CompositorState(CompositorToUiChannel toUi) => ToUi = toUi;

    public CompositorToUiChannel ToUi { get; }

    public uint NextSerial() => ++_serial;
    public uint NextSurfaceId() => ++_surfaceId;
    // Host ids are monotonic and NEVER reused within a session. Several safety arguments rely on this (e.g. a
    // late DismissPopup for an already-unmapped popup resolves to null rather than hitting a recycled id).
    public uint NextHostId() => ++_hostId;

    public void RegisterSurface(SurfaceState state)
    {
        _surfacesByResource[state.Resource] = state;
        _surfacesById[state.Id] = state;
    }

    public void UnregisterSurface(SurfaceState state)
    {
        _surfacesByResource.Remove(state.Resource);
        _surfacesById.Remove(state.Id);
    }

    public SurfaceState? GetSurface(WlSurface.Server? resource)
        => resource is not null ? _surfacesByResource.GetValueOrDefault(resource) : null;

    public SurfaceState? GetSurfaceById(uint id) => _surfacesById.GetValueOrDefault(id);

    // hostId → toplevel: lets UI→compositor jobs (close/resize proxying) resolve the role behind an opaque id.
    public void RegisterToplevelHost(uint hostId, XdgToplevelState toplevel) => _toplevelsByHostId[hostId] = toplevel;
    public void UnregisterToplevelHost(uint hostId) => _toplevelsByHostId.Remove(hostId);
    public XdgToplevelState? GetToplevelByHostId(uint hostId) => _toplevelsByHostId.GetValueOrDefault(hostId);

    // hostId → popup: lets the UI→compositor dismiss job (Avalonia Popup.Closed → xdg_popup.popup_done) resolve it.
    public void RegisterPopupHost(uint hostId, XdgPopupState popup) => _popupsByHostId[hostId] = popup;
    public void UnregisterPopupHost(uint hostId) => _popupsByHostId.Remove(hostId);
    public XdgPopupState? GetPopupByHostId(uint hostId) => _popupsByHostId.GetValueOrDefault(hostId);

    // hostId → the xdg_surface carrying its render root, whether a toplevel OR a popup. Input delivery resolves
    // through this so pointer/keyboard/text events reach popups too (they're not in the toplevel registry).
    public XdgSurfaceState? GetRenderRootByHostId(uint hostId)
        => GetToplevelByHostId(hostId)?.XdgSurface ?? GetPopupByHostId(hostId)?.XdgSurface;

    // xdg_surface resource → state: a popup's get_popup names its parent by the parent's xdg_surface resource;
    // resolving it lets popups anchor to a toplevel (or to another popup, for nested menus).
    public void RegisterXdgSurface(XdgSurface.Server resource, XdgSurfaceState state) => _xdgSurfacesByResource[resource] = state;
    public void UnregisterXdgSurface(XdgSurface.Server resource) => _xdgSurfacesByResource.Remove(resource);
    public XdgSurfaceState? GetXdgSurface(XdgSurface.Server? resource)
        => resource is not null ? _xdgSurfacesByResource.GetValueOrDefault(resource) : null;

    // xdg_positioner resource → its listener, so get_popup can snapshot the (mutable) positioner parameters that
    // were set on it (xdg requires the positioner be fully configured before get_popup; we copy them then).
    public void RegisterPositioner(XdgPositioner.Server resource, XdgPositionerListener listener) => _positionersByResource[resource] = listener;
    public void UnregisterPositioner(XdgPositioner.Server resource) => _positionersByResource.Remove(resource);
    public XdgPositionerListener? GetPositioner(XdgPositioner.Server? resource)
        => resource is not null ? _positionersByResource.GetValueOrDefault(resource) : null;

    // xdg-foreign (scenarios 3 & 4): an exported toplevel is published under an opaque handle string the client
    // hands (out of band) to another client, which imports it and uses it as a parent. Handles are minted here so
    // they're unique across all clients of this compositor.
    public string RegisterForeignExport(XdgToplevelState toplevel)
    {
        var handle = "avln-foreign-" + (++_foreignSeq);
        _foreignHandles[handle] = toplevel;
        return handle;
    }

    public XdgToplevelState? ResolveForeignHandle(string handle) => _foreignHandles.GetValueOrDefault(handle);
    public void RevokeForeignHandle(string handle) => _foreignHandles.Remove(handle);

    // Scenario 4: resolve an exported handle to the host id of the control hosting that toplevel, for the UI-side
    // ImportForeignXdgToplevel. 0 when the handle is unknown/revoked or the toplevel isn't mapped (no host yet).
    public uint ResolveForeignImportHostId(string handle)
        => _foreignHandles.GetValueOrDefault(handle) is { IsMapped: true } t ? t.HostId : 0;

    // Scenario 3 (xdg-foreign out): a HOST-side handle that refers to an Avalonia Window (not a client toplevel),
    // minted UI-side by ExportForeignXdgToplevel. The compositor only needs to know the handle is a valid
    // host-window export so an importing client's set_parent_of records it; the UI resolves handle→Window.
    private readonly HashSet<string> _hostWindowHandles = new();
    public void RegisterHostWindowExport(string handle) => _hostWindowHandles.Add(handle);
    public bool IsHostWindowHandle(string handle) => _hostWindowHandles.Contains(handle);
    public void RevokeHostWindowExport(string handle) => _hostWindowHandles.Remove(handle);

    // Scenario 1: a host control mints a token; we allocate its host id here so embed_toplevel(token) can
    // resolve the same id the control later renders under.
    public uint RegisterEmbedToken(string token)
    {
        var hostId = NextHostId();
        _embedTokens[token] = hostId;
        return hostId;
    }

    public bool TryResolveEmbedToken(string token, out uint hostId) => _embedTokens.TryGetValue(token, out hostId);

    /// <summary>One-shot: drop a token once its embed has resolved, so it can't be reused and doesn't leak.</summary>
    public void ConsumeEmbedToken(string token) => _embedTokens.Remove(token);

    // Scenario 5 (mark_content_surface): an Avalonia content host registers a cookie; the toolkit later tags its
    // container surface with it. The registration is DURABLE, not one-shot — the toolkit may mark its surface
    // BEFORE it maps (a realized-but-unmapped xdg_toplevel), in which case the host id doesn't exist yet and the
    // binding is resolved lazily when the toplevel maps (XdgToplevelState.Map). We only need to know the cookie is
    // live (the UI holds cookie→content-host); the cookie carries no host id — the target is the marked surface's
    // render root. The cookie is dropped when the content host detaches or is collected (UnregisterContentCookie).
    public void RegisterContentCookie(string cookie) => _contentCookies.Add(cookie);

    /// <summary>True if the cookie is currently registered (live). Does NOT consume it — a content cookie binds a
    /// content host for its lifetime, surviving a mark so a deferred (pre-map) mark can resolve on the toplevel's map.</summary>
    public bool IsContentCookieRegistered(string cookie) => _contentCookies.Contains(cookie);

    /// <summary>Drop a content cookie (the content host detached or was collected); a later mark/map for it is inert.</summary>
    public void UnregisterContentCookie(string cookie) => _contentCookies.Remove(cookie);
}
