using NWayland;
using NWayland.Protocols.Plasma.ServerDecoration;
using NWayland.Protocols.TextInputUnstableV3;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgForeignUnstableV1;
using NWayland.Protocols.XdgForeignUnstableV2;
using AvaloniaEmbedV1 = Avalonia.Wayland.Embedding.Protocol.AvaloniaEmbedV1;

namespace Avalonia.Wayland.Embedding.Compositor;

// P1 stubs: just enough to let GTK bind each global and initialize. Real input lands in P2 (D3);
// foreign/embedder land with their scenarios in P3 / increment 2. Decoration is the exception — it does
// real work now (forces SSD per D9) so GTK drops its client-side decorations.

internal sealed class OutputListener : WlOutput.ServerListener
{
    protected override void Release(WlOutput.Server resource) => resource.Dispose();
}

internal sealed class SeatListener : WlSeat.ServerListener
{
    private readonly ClientContext _client;
    public SeatListener(ClientContext client) => _client = client;

    protected override void GetPointer(WlSeat.Server resource, NewId<WlPointer.Server, WlPointer.ServerListener> id)
    {
        var listener = new PointerListener(_client);
        var pointer = id.GetAndConsume(listener);
        listener.Init(pointer);
        _client.RegisterPointer(pointer);
    }

    protected override void GetKeyboard(WlSeat.Server resource, NewId<WlKeyboard.Server, WlKeyboard.ServerListener> id)
    {
        var listener = new KeyboardListener(_client);
        var keyboard = id.GetAndConsume(listener);
        listener.Init(keyboard);
        _client.RegisterKeyboard(keyboard);
        try
        {
            var fd = XkbKeymap.CreateFd(out var size); // ownership transfers to NWayland on send
            keyboard.Keymap(WlKeyboard.KeymapFormatEnum.XkbV1, fd, size);
        }
        catch { /* keymap unavailable (no libxkbcommon) — raw key events still flow */ }
    }

    protected override void GetTouch(WlSeat.Server resource, NewId<WlTouch.Server, WlTouch.ServerListener> id)
        => id.GetAndConsume(new TouchListener());

    protected override void Release(WlSeat.Server resource) => resource.Dispose();
}

internal sealed class PointerListener : WlPointer.ServerListener
{
    private readonly ClientContext _client;
    private WlPointer.Server? _pointer;

    public PointerListener(ClientContext client) => _client = client;
    public void Init(WlPointer.Server pointer) => _pointer = pointer;

    protected override void SetCursor(WlPointer.Server resource, uint serial, WlSurface.Server? surface, int hotspotX, int hotspotY)
    {
        // Record the client's chosen cursor surface + hotspot; capture its buffer as the host control's cursor.
        var cursorSurface = _client.State.GetSurface(surface);
        _client.CursorSurface = cursorSurface;
        _client.CursorHotspotX = hotspotX;
        _client.CursorHotspotY = hotspotY;
        var hostId = _client.CurrentPointerHostId;
        if (hostId == 0)
            return; // the pointer isn't over a host right now — the request takes effect on the next enter
        if (cursorSurface is null)
            _client.State.ToUi.Enqueue(new CursorChangedEvent(hostId, null, 0, 0)); // null surface ⇒ revert to default
        else if (cursorSurface.HasFreshBitmap)
            cursorSurface.DeliverCursor(_client.State, hostId, hotspotX, hotspotY); // already committed its buffer
        // else: the cursor surface hasn't committed a buffer yet — its commit will deliver the image
    }

    protected override void Release(WlPointer.Server resource)
    {
        if (_pointer is not null)
            _client.UnregisterPointer(_pointer);
        resource.Dispose();
    }
}

internal sealed class KeyboardListener : WlKeyboard.ServerListener
{
    private readonly ClientContext _client;
    private WlKeyboard.Server? _keyboard;

    public KeyboardListener(ClientContext client) => _client = client;
    public void Init(WlKeyboard.Server keyboard) => _keyboard = keyboard;

    protected override void Release(WlKeyboard.Server resource)
    {
        if (_keyboard is not null)
            _client.UnregisterKeyboard(_keyboard);
        resource.Dispose();
    }
}

internal sealed class TouchListener : WlTouch.ServerListener
{
    protected override void Release(WlTouch.Server resource) => resource.Dispose();
}

internal sealed class DataDeviceManagerListener : WlDataDeviceManager.ServerListener
{
    protected override void CreateDataSource(WlDataDeviceManager.Server resource, NewId<WlDataSource.Server, WlDataSource.ServerListener> id)
        => id.GetAndConsume(new DataSourceListener());

    protected override void GetDataDevice(WlDataDeviceManager.Server resource, NewId<WlDataDevice.Server, WlDataDevice.ServerListener> id, WlSeat.Server? seat)
        => id.GetAndConsume(new DataDeviceListener());

    protected override void Release(WlDataDeviceManager.Server resource) => resource.Dispose();
}

internal sealed class DataSourceListener : WlDataSource.ServerListener
{
    protected override void Offer(WlDataSource.Server resource, string mimeType) { }
    protected override void SetActions(WlDataSource.Server resource, WlDataDeviceManager.DndActionEnum dndActions) { }
    protected override void Destroy(WlDataSource.Server resource) => resource.Dispose();
}

internal sealed class DataDeviceListener : WlDataDevice.ServerListener
{
    protected override void StartDrag(WlDataDevice.Server resource, WlDataSource.Server? source, WlSurface.Server? origin, WlSurface.Server? icon, uint serial) { }
    protected override void SetSelection(WlDataDevice.Server resource, WlDataSource.Server? source, uint serial) { }
    protected override void Release(WlDataDevice.Server resource) => resource.Dispose();
}

internal sealed class TextInputManagerListener : ZwpTextInputManagerV3.ServerListener
{
    private readonly ClientContext _client;
    public TextInputManagerListener(ClientContext client) => _client = client;

    protected override void GetTextInput(ZwpTextInputManagerV3.Server resource, NewId<ZwpTextInputV3.Server, ZwpTextInputV3.ServerListener> id, WlSeat.Server? seat)
    {
        var listener = new TextInputListener(_client);
        var textInput = id.GetAndConsume(listener);
        var state = new TextInputState(textInput);
        listener.Init(state);
        _client.RegisterTextInput(state);
    }

    protected override void Destroy(ZwpTextInputManagerV3.Server resource) => resource.Dispose();
}

/// <summary>Per-text-input state the input-delivery path reads. Compositor-thread only.</summary>
internal sealed class TextInputState
{
    public TextInputState(ZwpTextInputV3.Server resource) => Resource = resource;
    public ZwpTextInputV3.Server Resource { get; }
    public bool Enabled { get; set; }
    public uint CommitSerial { get; set; } // counts the client's `commit` requests; echoed in done(serial)
}

internal sealed class TextInputListener : ZwpTextInputV3.ServerListener
{
    private readonly ClientContext _client;
    private TextInputState _state = null!;

    public TextInputListener(ClientContext client) => _client = client;
    public void Init(TextInputState state) => _state = state;

    protected override void Enable(ZwpTextInputV3.Server resource) => _state.Enabled = true;
    protected override void Disable(ZwpTextInputV3.Server resource) => _state.Enabled = false;
    protected override void SetSurroundingText(ZwpTextInputV3.Server resource, string text, int cursor, int anchor) { }
    protected override void SetTextChangeCause(ZwpTextInputV3.Server resource, ZwpTextInputV3.ChangeCauseEnum cause) { }
    protected override void SetContentType(ZwpTextInputV3.Server resource, ZwpTextInputV3.ContentHintEnum hint, ZwpTextInputV3.ContentPurposeEnum purpose) { }
    protected override void SetCursorRectangle(ZwpTextInputV3.Server resource, int x, int y, int width, int height)
    {
        // Reverse IME: surface the toolkit's caret rectangle to the host's IME bridge so the OS candidate window
        // tracks the embedded caret. Routed to whichever host the text-input is focused on.
        var hostId = _client.TextInputFocusHostId;
        if (hostId != 0)
            _client.State.ToUi.Enqueue(new TextInputCursorRectEvent(hostId, x, y, width, height));
    }
    protected override void SetAvailableActions(ZwpTextInputV3.Server resource, System.ReadOnlySpan<byte> availableActions) { }
    protected override void ShowInputPanel(ZwpTextInputV3.Server resource) { }
    protected override void HideInputPanel(ZwpTextInputV3.Server resource) { }
    protected override void Commit(ZwpTextInputV3.Server resource) => _state.CommitSerial++;

    protected override void Destroy(ZwpTextInputV3.Server resource)
    {
        _client.UnregisterTextInput(_state);
        resource.Dispose();
    }
}

internal sealed class ServerDecorationManagerListener : OrgKdeKwinServerDecorationManager.ServerListener
{
    protected override void Create(OrgKdeKwinServerDecorationManager.Server resource,
        NewId<OrgKdeKwinServerDecoration.Server, OrgKdeKwinServerDecoration.ServerListener> id, WlSurface.Server? surface)
    {
        var decoration = id.GetAndConsume(new ServerDecorationListener());
        // D9: tell the toolkit the server owns decorations so it drops its CSD shadow/frame.
        decoration.Mode((uint)OrgKdeKwinServerDecoration.ModeEnum.Server);
    }
}

internal sealed class ServerDecorationListener : OrgKdeKwinServerDecoration.ServerListener
{
    protected override void RequestMode(OrgKdeKwinServerDecoration.Server resource, uint mode)
        // We always answer Server regardless of the request — Avalonia's X11 host has broken CSD extents.
        => resource.Mode((uint)OrgKdeKwinServerDecoration.ModeEnum.Server);

    protected override void Release(OrgKdeKwinServerDecoration.Server resource) => resource.Dispose();
}

// xdg-foreign (scenarios 3 & 4): a client exports its toplevel and receives an opaque handle; another client
// imports the handle and makes its own toplevel a child of the exported one (set_parent_of → owner mapping).
// We serve BOTH the v2 and v1 globals: v2 for Wayland-native clients, v1 because GTK3 only ever binds v1. The
// version-specific listeners below are thin shells around the shared, version-agnostic logic in ForeignSupport.
internal static class ForeignSupport
{
    // Resolve a surface's xdg_toplevel role and register an export handle for it, wiring revoke-on-toplevel-destroy.
    // Returns the handle to send in the exported.handle event, or null when the surface has no toplevel role
    // (⇒ inert export object, no handle event, which the client reads as a failed export).
    public static string? BeginExport(ClientContext client, WlSurface.Server? surface, out XdgToplevelState? toplevel)
    {
        toplevel = client.State.GetSurface(surface)?.XdgSurface?.Toplevel;
        if (toplevel is null)
            return null;
        var handle = client.State.RegisterForeignExport(toplevel);
        toplevel.AddExportHandle(handle); // so the toplevel's Destroy revokes it if the export outlives the role
        return handle;
    }

    public static void RevokeExport(ClientContext client, string? handle, XdgToplevelState? toplevel)
    {
        if (handle is null)
            return;
        client.State.RevokeForeignHandle(handle); // unpublish so a later import of this handle is inert
        toplevel?.RemoveExportHandle(handle);
    }

    // Resolve an imported handle to either a client toplevel (scenario 4) or a host-side Avalonia Window export
    // (scenario 3). An unknown/revoked handle resolves to neither ⇒ the imported object is inert.
    public static (XdgToplevelState? parentToplevel, string? parentWindowHandle) ResolveImport(ClientContext client, string handle)
    {
        var parentToplevel = client.State.ResolveForeignHandle(handle);
        var parentWindowHandle = parentToplevel is null && client.State.IsHostWindowHandle(handle) ? handle : null;
        return (parentToplevel, parentWindowHandle);
    }

    public static void ApplyParent(ClientContext client, WlSurface.Server? surface, XdgToplevelState? parentToplevel, string? parentWindowHandle)
    {
        if (parentToplevel is null && parentWindowHandle is null)
            return; // inert import (unknown handle)
        var child = client.State.GetSurface(surface)?.XdgSurface?.Toplevel;
        if (child is null)
            return;
        // Record the parent so the child's Map() owns its auto-window from creation — the ordering real toolkits
        // use (set_parent_of before mapping the child). A late set_parent_of (child already shown) can't re-own
        // the window — Avalonia exposes no API for it — so recording it is intentionally the only effect.
        if (parentToplevel is not null)
            child.SetForeignParent(parentToplevel);
        else
            child.SetForeignParentWindow(parentWindowHandle!);
    }
}

internal sealed class ForeignExporterListenerV2 : ZxdgExporterV2.ServerListener
{
    private readonly ClientContext _client;
    public ForeignExporterListenerV2(ClientContext client) => _client = client;

    protected override void ExportToplevel(ZxdgExporterV2.Server resource, NewId<ZxdgExportedV2.Server, ZxdgExportedV2.ServerListener> id, WlSurface.Server? surface)
    {
        var handle = ForeignSupport.BeginExport(_client, surface, out var toplevel);
        var exported = id.GetAndConsume(new ForeignExportedListenerV2(_client, toplevel, handle));
        if (handle is not null)
            exported.Handle(handle);
    }

    protected override void Destroy(ZxdgExporterV2.Server resource) => resource.Dispose();
}

internal sealed class ForeignExportedListenerV2 : ZxdgExportedV2.ServerListener
{
    private readonly ClientContext _client;
    private readonly XdgToplevelState? _toplevel;
    private readonly string? _handle;

    public ForeignExportedListenerV2(ClientContext client, XdgToplevelState? toplevel, string? handle)
    {
        _client = client;
        _toplevel = toplevel;
        _handle = handle;
    }

    protected override void Destroy(ZxdgExportedV2.Server resource)
    {
        ForeignSupport.RevokeExport(_client, _handle, _toplevel);
        resource.Dispose();
    }
}

internal sealed class ForeignImporterListenerV2 : ZxdgImporterV2.ServerListener
{
    private readonly ClientContext _client;
    public ForeignImporterListenerV2(ClientContext client) => _client = client;

    protected override void ImportToplevel(ZxdgImporterV2.Server resource, NewId<ZxdgImportedV2.Server, ZxdgImportedV2.ServerListener> id, string handle)
    {
        var (parentToplevel, parentWindowHandle) = ForeignSupport.ResolveImport(_client, handle);
        id.GetAndConsume(new ForeignImportedListenerV2(_client, parentToplevel, parentWindowHandle));
    }

    protected override void Destroy(ZxdgImporterV2.Server resource) => resource.Dispose();
}

internal sealed class ForeignImportedListenerV2 : ZxdgImportedV2.ServerListener
{
    private readonly ClientContext _client;
    private readonly XdgToplevelState? _parentToplevel;     // scenario 4: parent is another client's toplevel
    private readonly string? _parentWindowHandle;           // scenario 3: parent is an exported Avalonia Window

    public ForeignImportedListenerV2(ClientContext client, XdgToplevelState? parentToplevel, string? parentWindowHandle)
    {
        _client = client;
        _parentToplevel = parentToplevel;
        _parentWindowHandle = parentWindowHandle;
    }

    protected override void SetParentOf(ZxdgImportedV2.Server resource, WlSurface.Server? surface)
        => ForeignSupport.ApplyParent(_client, surface, _parentToplevel, _parentWindowHandle);

    protected override void Destroy(ZxdgImportedV2.Server resource) => resource.Dispose();
}

// xdg-foreign-unstable-v1 mirrors. v1 names the requests `export`/`import` (vs v2's `export_toplevel`/
// `import_toplevel`); the wire handles and our resolution are otherwise identical, so these delegate to
// ForeignSupport. GTK3 binds these (it has no v2 support), so scenarios 3 & 4 ride this path with real GTK.
internal sealed class ForeignExporterListenerV1 : ZxdgExporterV1.ServerListener
{
    private readonly ClientContext _client;
    public ForeignExporterListenerV1(ClientContext client) => _client = client;

    protected override void Export(ZxdgExporterV1.Server resource, NewId<ZxdgExportedV1.Server, ZxdgExportedV1.ServerListener> id, WlSurface.Server? surface)
    {
        var handle = ForeignSupport.BeginExport(_client, surface, out var toplevel);
        var exported = id.GetAndConsume(new ForeignExportedListenerV1(_client, toplevel, handle));
        if (handle is not null)
            exported.Handle(handle);
    }

    protected override void Destroy(ZxdgExporterV1.Server resource) => resource.Dispose();
}

internal sealed class ForeignExportedListenerV1 : ZxdgExportedV1.ServerListener
{
    private readonly ClientContext _client;
    private readonly XdgToplevelState? _toplevel;
    private readonly string? _handle;

    public ForeignExportedListenerV1(ClientContext client, XdgToplevelState? toplevel, string? handle)
    {
        _client = client;
        _toplevel = toplevel;
        _handle = handle;
    }

    protected override void Destroy(ZxdgExportedV1.Server resource)
    {
        ForeignSupport.RevokeExport(_client, _handle, _toplevel);
        resource.Dispose();
    }
}

internal sealed class ForeignImporterListenerV1 : ZxdgImporterV1.ServerListener
{
    private readonly ClientContext _client;
    public ForeignImporterListenerV1(ClientContext client) => _client = client;

    protected override void Import(ZxdgImporterV1.Server resource, NewId<ZxdgImportedV1.Server, ZxdgImportedV1.ServerListener> id, string handle)
    {
        var (parentToplevel, parentWindowHandle) = ForeignSupport.ResolveImport(_client, handle);
        id.GetAndConsume(new ForeignImportedListenerV1(_client, parentToplevel, parentWindowHandle));
    }

    protected override void Destroy(ZxdgImporterV1.Server resource) => resource.Dispose();
}

internal sealed class ForeignImportedListenerV1 : ZxdgImportedV1.ServerListener
{
    private readonly ClientContext _client;
    private readonly XdgToplevelState? _parentToplevel;     // scenario 4: parent is another client's toplevel
    private readonly string? _parentWindowHandle;           // scenario 3: parent is an exported Avalonia Window

    public ForeignImportedListenerV1(ClientContext client, XdgToplevelState? parentToplevel, string? parentWindowHandle)
    {
        _client = client;
        _parentToplevel = parentToplevel;
        _parentWindowHandle = parentWindowHandle;
    }

    protected override void SetParentOf(ZxdgImportedV1.Server resource, WlSurface.Server? surface)
        => ForeignSupport.ApplyParent(_client, surface, _parentToplevel, _parentWindowHandle);

    protected override void Destroy(ZxdgImportedV1.Server resource) => resource.Dispose();
}

internal sealed class EmbedderListener : AvaloniaEmbedV1.AvaloniaEmbedder.ServerListener
{
    private readonly ClientContext _client;
    public EmbedderListener(ClientContext client) => _client = client;

    protected override void EmbedToplevel(AvaloniaEmbedV1.AvaloniaEmbedder.Server resource,
        NewId<AvaloniaEmbedV1.AvaloniaEmbedResult.Server, AvaloniaEmbedV1.AvaloniaEmbedResult.ServerListener> id,
        WlSurface.Server? surface, string token)
    {
        // Scenario 1: resolve the token to a pre-registered host id and bind the toplevel to it (it then
        // renders into that Avalonia control instead of an auto-window). Must run BEFORE the toplevel maps —
        // _embeddedHostId is consulted only in Map(), so embedding an already-mapped (or already-embedded)
        // toplevel can't take effect and is rejected rather than falsely reported as bound.
        var result = id.GetAndConsume(new EmbedResultListener());
        var toplevel = _client.State.GetSurface(surface)?.XdgSurface?.Toplevel;
        if (toplevel is not null && !toplevel.IsMapped && !toplevel.IsEmbedded
            && token is not null && _client.State.TryResolveEmbedToken(token, out var hostId))
        {
            toplevel.SetEmbeddedHost(hostId);
            _client.State.ConsumeEmbedToken(token); // one-shot
            result.Bound();
        }
        else
        {
            result.Rejected(); // unknown token, no xdg_toplevel role, already mapped, or already embedded
        }
    }

    protected override void MarkContentSurface(AvaloniaEmbedV1.AvaloniaEmbedder.Server resource,
        NewId<AvaloniaEmbedV1.AvaloniaEmbedResult.Server, AvaloniaEmbedV1.AvaloniaEmbedResult.ServerListener> id,
        WlSurface.Server? surface, string cookie)
    {
        // Scenario 5: bind the Avalonia content host that minted `cookie` to the host control rendering this
        // toolkit window. `surface` is the window's OWN surface (not a placeholder); resolve it to its render
        // root and require a TOPLEVEL role — a popup is a transient host torn down on dismiss, not a content
        // container. The surface need NOT be mapped yet: the relationship is recorded on the toplevel and resolved
        // when it maps (its host id only exists at map), so the toolkit can mark a realized-but-unmapped window
        // before it draws — symmetric with embed_toplevel. The cookie must be a live (registered) one.
        var result = id.GetAndConsume(new EmbedResultListener());
        var toplevel = _client.State.GetSurface(surface)?.FindRenderRoot()?.Toplevel;
        if (cookie is not null && toplevel is not null && _client.State.IsContentCookieRegistered(cookie))
        {
            toplevel.SetContentCookie(cookie);
            if (toplevel.IsMapped) // already mapped → resolve now; otherwise Map() emits it once the host id exists
                _client.State.ToUi.Enqueue(new ContentSurfaceMarkedEvent(cookie, toplevel.HostId));
            result.Bound();
        }
        else
        {
            result.Rejected(); // unknown/dropped cookie, or the surface has no xdg_toplevel role (popup/bare surface)
        }
    }

    protected override void Associate(AvaloniaEmbedV1.AvaloniaEmbedder.Server resource, uint ticket)
        // Bind this connection to the Avalonia-side ticket; carried on each of its toplevels' map events so the UI
        // can scope a surface's object id to this connection when matching resized widgets.
        => _client.Ticket = ticket;

    protected override void Destroy(AvaloniaEmbedV1.AvaloniaEmbedder.Server resource) => resource.Dispose();
}

internal sealed class EmbedResultListener : AvaloniaEmbedV1.AvaloniaEmbedResult.ServerListener
{
    protected override void Destroy(AvaloniaEmbedV1.AvaloniaEmbedResult.Server resource) => resource.Dispose();
}
