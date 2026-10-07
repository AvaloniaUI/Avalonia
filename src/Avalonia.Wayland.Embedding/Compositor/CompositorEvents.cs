using Avalonia.Media.Imaging;
using Avalonia.Wayland.Embedding.Hosting;

namespace Avalonia.Wayland.Embedding.Compositor;

// (Avalonia.Size is referenced below for the embedded min/max constraints.)

/// <summary>compositor→UI: an auto-hosted toplevel mapped; the UI creates a window hosting its surface tree.</summary>
internal sealed class ToplevelMappedEvent : CompositorToUiEvent
{
    public ToplevelMappedEvent(uint hostId, string title, string appId,
        int minWidth, int minHeight, int maxWidth, int maxHeight, int initialWidth, int initialHeight,
        uint parentHostId = 0, string? parentWindowHandle = null, uint surfaceObjectId = 0, uint connectionTicket = 0)
    {
        HostId = hostId;
        Title = title;
        AppId = appId;
        MinWidth = minWidth;
        MinHeight = minHeight;
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
        InitialWidth = initialWidth;
        InitialHeight = initialHeight;
        ParentHostId = parentHostId;
        ParentWindowHandle = parentWindowHandle;
        SurfaceObjectId = surfaceObjectId;
        ConnectionTicket = connectionTicket;
    }

    public uint HostId { get; }
    public string Title { get; }
    public string AppId { get; }
    public int MinWidth { get; }
    public int MinHeight { get; }
    public int MaxWidth { get; }
    public int MaxHeight { get; }
    /// <summary>The toplevel's logical size at map (geometry, else surface size), in DIPs; used to size the auto-window.</summary>
    public int InitialWidth { get; }
    public int InitialHeight { get; }
    /// <summary>xdg-foreign owner: the host id of the toplevel this one was made a child of (0 = none). The
    /// auto-window is shown owned by that parent's window so it stacks above and centers like a real dialog.</summary>
    public uint ParentHostId { get; }
    /// <summary>xdg-foreign owner (scenario 3): the host-side handle of an exported Avalonia Window this toplevel was
    /// made a child of (null = none). Takes precedence over <see cref="ParentHostId"/> when resolving the owner.</summary>
    public string? ParentWindowHandle { get; }
    /// <summary>The toplevel surface's wayland object id (matches the client's <c>wl_proxy_get_id</c>), so the UI can
    /// map a toolkit window back to this host with no client round-trip — used to target resized widgets.</summary>
    public uint SurfaceObjectId { get; }
    /// <summary>The connection's <c>WaylandEmbedderConnection</c> ticket (0 if none), scoping <see cref="SurfaceObjectId"/>
    /// to this connection (object ids are only unique per connection).</summary>
    public uint ConnectionTicket { get; }

    /// <summary>Min size in DIPs, or null when the client set no minimum (both dimensions 0, per xdg).</summary>
    public Size? MinSize => MinWidth > 0 && MinHeight > 0 ? new Size(MinWidth, MinHeight) : null;
    /// <summary>Max size in DIPs, or null when the client set no maximum.</summary>
    public Size? MaxSize => MaxWidth > 0 && MaxHeight > 0 ? new Size(MaxWidth, MaxHeight) : null;

    public override void Apply() => WaylandHosting.OnToplevelMapped(this);
}

/// <summary>compositor→UI: the client unmapped/destroyed its toplevel; the UI closes the auto-window.</summary>
internal sealed class ToplevelUnmappedEvent : CompositorToUiEvent
{
    public ToplevelUnmappedEvent(uint hostId) => HostId = hostId;
    public uint HostId { get; }
    public override void Apply() => WaylandHosting.OnToplevelUnmapped(HostId);
}

/// <summary>compositor→UI: an xdg_popup mapped; the UI opens an Avalonia Popup anchored to the parent host,
/// hosting the popup's own surface tree (own host id) with light-dismiss enabled when the client grabbed.</summary>
internal sealed class PopupMappedEvent : CompositorToUiEvent
{
    public PopupMappedEvent(uint hostId, uint parentHostId, int x, int y, int width, int height,
        bool isGrab, PositionerSnapshot positioner)
    {
        HostId = hostId;
        ParentHostId = parentHostId;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        IsGrab = isGrab;
        Positioner = positioner;
    }

    public uint HostId { get; }
    public uint ParentHostId { get; }
    /// <summary>Popup geometry relative to the parent's window-geometry origin, in DIPs (pre-constraint).</summary>
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    /// <summary>The client requested a grab ⇒ map to an Avalonia light-dismiss popup.</summary>
    public bool IsGrab { get; }
    /// <summary>The full positioner snapshot, so the UI can place via Avalonia's anchor/gravity engine.</summary>
    public PositionerSnapshot Positioner { get; }

    public override void Apply() => WaylandHosting.OnPopupMapped(this);
}

/// <summary>compositor→UI: the client destroyed its xdg_popup (or the parent went away); close its Avalonia Popup.</summary>
internal sealed class PopupUnmappedEvent : CompositorToUiEvent
{
    public PopupUnmappedEvent(uint hostId) => HostId = hostId;
    public uint HostId { get; }
    public override void Apply() => WaylandHosting.OnPopupUnmapped(HostId);
}

/// <summary>compositor→UI: an xdg_popup.reposition re-placed a mapped popup; move its live Avalonia Popup to match.</summary>
internal sealed class PopupRepositionedEvent : CompositorToUiEvent
{
    public PopupRepositionedEvent(uint hostId, int x, int y, int width, int height, PositionerSnapshot positioner)
    {
        HostId = hostId;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Positioner = positioner;
    }

    public uint HostId { get; }
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public PositionerSnapshot Positioner { get; }

    public override void Apply() => WaylandHosting.OnPopupRepositioned(this);
}

/// <summary>compositor→UI (scenario 5): the toolkit marked its container surface with a content cookie; attach the
/// Avalonia content host that minted the cookie onto the host control rendering that surface (resolved host id).</summary>
internal sealed class ContentSurfaceMarkedEvent : CompositorToUiEvent
{
    public ContentSurfaceMarkedEvent(string cookie, uint hostId)
    {
        Cookie = cookie;
        HostId = hostId;
    }

    public string Cookie { get; }
    public uint HostId { get; }
    public override void Apply() => WaylandHosting.OnContentSurfaceMarked(Cookie, HostId);
}

/// <summary>compositor→UI: the client set its pointer cursor (wl_pointer.set_cursor). Carries the captured cursor
/// image (ownership transferred; null = revert to the host default) + hotspot, for the host the pointer is over.</summary>
internal sealed class CursorChangedEvent : CompositorToUiEvent
{
    public CursorChangedEvent(uint hostId, Bitmap? cursor, int hotspotX, int hotspotY)
    {
        HostId = hostId;
        Cursor = cursor;
        HotspotX = hotspotX;
        HotspotY = hotspotY;
    }

    public uint HostId { get; }
    public Bitmap? Cursor { get; }
    public int HotspotX { get; }
    public int HotspotY { get; }
    public override void Apply() => WaylandHosting.OnCursorChanged(this);
}

/// <summary>compositor→UI: the embedded client reported its IME caret rectangle (zwp_text_input_v3.set_cursor_rectangle,
/// in surface coords); the host maps it into host coords and surfaces it to the OS IME via its bridge.</summary>
internal sealed class TextInputCursorRectEvent : CompositorToUiEvent
{
    public TextInputCursorRectEvent(uint hostId, int x, int y, int width, int height)
    {
        HostId = hostId;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public uint HostId { get; }
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public override void Apply() => WaylandHosting.OnTextInputCursorRect(this);
}

/// <summary>compositor→UI: a new committed surface-tree snapshot for a host; the UI view repaints.</summary>
internal sealed class SurfaceCommitEvent : CompositorToUiEvent
{
    public SurfaceCommitEvent(SurfaceCommit commit) => Commit = commit;
    public SurfaceCommit Commit { get; }
    public override void Apply() => WaylandHosting.OnSurfaceCommit(Commit);
}
