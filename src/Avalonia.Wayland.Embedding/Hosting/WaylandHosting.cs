using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Threading;
using Avalonia.Wayland.Embedding.Compositor;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Embedding.Hosting;

/// <summary>
/// UI-thread registry of host controls keyed by the compositor's opaque host id. Touched only on the UI thread
/// (all callers are compositor→UI events applied during the drain), so no locking is needed. For scenario 2
/// (auto-hosted toplevels) it also manufactures the Avalonia window that hosts the surface tree, and for popups
/// the Avalonia <see cref="Popup"/> anchored to the parent host.
/// </summary>
internal static class WaylandHosting
{
    private static readonly Dictionary<uint, WaylandSubcompositorControlHost> s_hosts = new();
    private static readonly Dictionary<uint, Window> s_autoWindows = new();
    private static readonly Dictionary<uint, Popup> s_popups = new();
    // cookie → content host, held WEAKLY: the content host is rooted by the glue control that owns it, so this map
    // never keeps it alive. When the glue drops it the weak ref goes dead (and the content host's finalizer posts
    // the compositor-side unregister), so a stale entry just resolves to a dead target and is swept. The relationship
    // is durable (kept across a mark, so a deferred pre-map mark resolves on the toplevel's map) — not one-shot.
    private static readonly Dictionary<string, WeakReference<WaylandSubcompositorAvaloniaContentHost>> s_contentCookies = new();
    // Scenario 3: Avalonia windows exported as foreign dialog parents, keyed by the handle ExportForeignXdgToplevel minted.
    private static readonly Dictionary<string, Window> s_exportedWindows = new();

    public static void OnToplevelMapped(ToplevelMappedEvent ev)
    {
        if (s_hosts.TryGetValue(ev.HostId, out var embedded))
        {
            // Scenario 1: a control pre-registered via GetEmbeddingToken — bind metadata + mark mapped, no window.
            embedded.SurfaceObjectId = ev.SurfaceObjectId;
            embedded.ConnectionTicket = ev.ConnectionTicket;
            embedded.SetMetadata(ev.Title, ev.AppId, ev.MinSize, ev.MaxSize);
            embedded.NotifyMapped();
            return;
        }

        // A toplevel fills its window, so keep it frame-perfect: draw the buffer 1:1 (the resize flush keeps it in
        // step with the window) instead of stretching a stale buffer during a resize round-trip.
        var host = new WaylandSubcompositorControlHost
        {
            HostId = ev.HostId, StretchContent = false, SurfaceObjectId = ev.SurfaceObjectId, ConnectionTicket = ev.ConnectionTicket,
        };
        host.SetMetadata(ev.Title, ev.AppId, ev.MinSize, ev.MaxSize);
        s_hosts[ev.HostId] = host;

        // Scenario 2: act like a rootless compositor — wrap the embedded toplevel in a real Avalonia window,
        // mirroring its title and size constraints. The window is sized to the toolkit's buffer size DIRECTLY
        // (not via Avalonia layout — the control imposes no content size); it stays user-resizable and the
        // control renders the surface stretched until the client re-renders at the configured size.
        var window = new Window
        {
            Title = string.IsNullOrEmpty(ev.Title) ? "Embedded" : ev.Title,
            Content = host,
        };
        if (ev.InitialWidth > 0 && ev.InitialHeight > 0)
        {
            window.Width = ev.InitialWidth;
            window.Height = ev.InitialHeight;
        }
        if (ev.MinSize is { } min)
        {
            window.MinWidth = min.Width;
            window.MinHeight = min.Height;
        }
        if (ev.MaxSize is { } max)
        {
            window.MaxWidth = max.Width;
            window.MaxHeight = max.Height;
        }
        s_autoWindows[ev.HostId] = window;
        // Scenario 2 close proxy: a user-initiated close only ASKS the client to close (it owns its lifecycle)
        // and is cancelled; the window really closes when the client destroys its surface → OnToplevelUnmapped,
        // which Stop()s the host first (clearing IsEmbeddedSurfaceMapped) so this handler no longer cancels.
        window.Closing += (_, e) =>
        {
            if (host.IsEmbeddedSurfaceMapped)
            {
                e.Cancel = true;
                host.RequestClose();
            }
        };
        // xdg-foreign (scenario 3/4): if this toplevel was made a child of another (set_parent_of before map),
        // own it to that parent's window so it stacks above and centers like a real dialog (non-modal — see risk #8).
        var owner = ResolveOwnerWindow(ev.ParentHostId, ev.ParentWindowHandle);
        if (owner is not null)
            window.Show(owner);
        else
            window.Show();
        host.NotifyMapped();
    }

    public static void OnToplevelUnmapped(uint hostId)
    {
        if (s_hosts.Remove(hostId, out var host))
            host.Stop();
        if (s_autoWindows.Remove(hostId, out var window))
            window.Close();
    }

    public static void OnPopupMapped(PopupMappedEvent ev)
    {
        if (!s_hosts.TryGetValue(ev.ParentHostId, out var parentHost))
        {
            // The parent host went away (raced with unmap). Don't leave the client's popup mapped and
            // frame-starved (no host ⇒ never rendered ⇒ frame callbacks never fire); tell it to dismiss.
            WaylandEmbeddingSubcompositor.Api.DismissPopup(ev.HostId);
            return;
        }

        // The host control imposes no layout size (MeasureOverride is empty), so give the popup an explicit size
        // from the positioner; the surface is rendered stretched into it (same model as the auto-window).
        var host = new WaylandSubcompositorControlHost
        {
            HostId = ev.HostId,
            Width = ev.Width > 0 ? ev.Width : double.NaN,
            Height = ev.Height > 0 ? ev.Height : double.NaN,
        };
        s_hosts[ev.HostId] = host;

        var popup = new Popup
        {
            Child = host,
            PlacementTarget = parentHost,
            Placement = PlacementMode.AnchorAndGravity,
            IsLightDismissEnabled = ev.IsGrab, // xdg grab ⇒ Avalonia light dismiss (click-outside closes it)
        };
        ApplyPositioner(popup, ev.Positioner);
        s_popups[ev.HostId] = popup;
        // Closed fires on light dismiss, programmatic close, or our own teardown. Sending popup_done is the
        // compositor→client "your popup was dismissed, destroy it" signal; after the client destroys the popup
        // (→ OnPopupUnmapped) the matching DismissPopup is a no-op (the popup host is already unregistered,
        // and host ids are never reused, so it can't hit a different popup).
        popup.Closed += (_, _) => WaylandEmbeddingSubcompositor.Api.DismissPopup(ev.HostId);
        ((ISetLogicalParent)popup).SetParent(parentHost);
        popup.Open();
        host.NotifyMapped();
    }

    public static void OnPopupUnmapped(uint hostId)
    {
        if (s_hosts.Remove(hostId, out var host))
            host.Stop();
        if (s_popups.Remove(hostId, out var popup))
        {
            popup.Close();
            ((ISetLogicalParent)popup).SetParent(null);
        }
    }

    /// <summary>
    /// xdg_popup.reposition re-placed a mapped popup: update the live Avalonia Popup so it doesn't stay at the
    /// old geometry. Changing PlacementRect/anchor/offsets re-positions an open popup in place (Popup re-runs its
    /// positioner on those property changes); changing Width/Height re-sizes it.
    /// </summary>
    public static void OnPopupRepositioned(PopupRepositionedEvent ev)
    {
        if (!s_popups.TryGetValue(ev.HostId, out var popup))
            return;
        if (popup.Child is WaylandSubcompositorControlHost host)
        {
            host.Width = ev.Width > 0 ? ev.Width : double.NaN;
            host.Height = ev.Height > 0 ? ev.Height : double.NaN;
        }
        ApplyPositioner(popup, ev.Positioner);
    }

    /// <summary>Map an xdg_positioner snapshot onto an Avalonia Popup's placement. PlacementRect is set last so
    /// its property-change triggers the (re)position using the anchor/gravity/offsets just assigned.</summary>
    private static void ApplyPositioner(Popup popup, PositionerSnapshot p)
    {
        popup.PlacementAnchor = ToAvaloniaAnchor(p.Anchor);
        popup.PlacementGravity = ToAvaloniaGravity(p.Gravity);
        popup.PlacementConstraintAdjustment = (PopupPositionerConstraintAdjustment)(uint)p.ConstraintAdjustment;
        popup.HorizontalOffset = p.OffsetX;
        popup.VerticalOffset = p.OffsetY;
        popup.PlacementRect = new Rect(p.RectX, p.RectY, p.RectWidth, p.RectHeight);
    }

    /// <summary>compositor→UI (set_cursor): apply the client's custom cursor image to the host the pointer is over.</summary>
    public static void OnCursorChanged(CursorChangedEvent ev)
    {
        if (s_hosts.TryGetValue(ev.HostId, out var host))
            host.SetEmbeddedCursor(ev.Cursor, ev.HotspotX, ev.HotspotY);
        else
            ev.Cursor?.Dispose(); // no host (raced with unmap) — don't leak the transferred bitmap
    }

    /// <summary>compositor→UI: the embedded client's IME caret rectangle; surface it to the host's IME bridge.</summary>
    public static void OnTextInputCursorRect(TextInputCursorRectEvent ev)
    {
        if (s_hosts.TryGetValue(ev.HostId, out var host))
            host.SetEmbeddedImeCursorRect(ev.X, ev.Y, ev.Width, ev.Height);
    }

    public static void OnSurfaceCommit(SurfaceCommit commit)
    {
        if (s_hosts.TryGetValue(commit.HostId, out var host))
            host.ApplyCommit(commit);
        else
            foreach (var bmp in commit.NewBitmaps.Values)
                bmp.Dispose(); // no host (raced with unmap) — don't leak the transferred bitmaps
    }

    /// <summary>Look up a host for an externally-created control (scenario 1, next increment). UI thread only.</summary>
    public static WaylandSubcompositorControlHost? GetHost(uint hostId)
    {
        Dispatcher.UIThread.VerifyAccess();
        return s_hosts.GetValueOrDefault(hostId);
    }

    /// <summary>Register a host for an externally-created control (scenario 1, next increment). UI thread only.</summary>
    public static void RegisterHost(uint hostId, WaylandSubcompositorControlHost host)
    {
        Dispatcher.UIThread.VerifyAccess();
        s_hosts[hostId] = host;
    }

    /// <summary>UI thread: the host control that a host id maps to, or null if the id is 0 or unknown.</summary>
    internal static WaylandSubcompositorControlHost? GetHostById(uint hostId)
    {
        Dispatcher.UIThread.VerifyAccess();
        return hostId == 0 ? null : s_hosts.GetValueOrDefault(hostId);
    }

    /// <summary>UI thread (scenario 3): register / drop an Avalonia Window exported as a foreign dialog parent.</summary>
    internal static void RegisterExportedWindow(string handle, Window window)
    {
        Dispatcher.UIThread.VerifyAccess();
        s_exportedWindows[handle] = window;
    }

    internal static void RevokeExportedWindow(string handle)
    {
        Dispatcher.UIThread.VerifyAccess();
        s_exportedWindows.Remove(handle);
    }

    /// <summary>UI thread: a content host registers its cookie after CreateAttachmentCookie posted it (scenario 5).
    /// Held weakly (the glue control roots the content host); sweeps any dead entries while we're here.</summary>
    internal static void RegisterContentCookie(string cookie, WaylandSubcompositorAvaloniaContentHost contentHost)
    {
        Dispatcher.UIThread.VerifyAccess();
        SweepDeadContentCookies();
        s_contentCookies[cookie] = new WeakReference<WaylandSubcompositorAvaloniaContentHost>(contentHost);
    }

    /// <summary>UI thread: drop a cookie (content host detached); a later mark/map for it resolves to nothing.</summary>
    internal static void RemoveContentCookie(string cookie) => s_contentCookies.Remove(cookie);

    /// <summary>compositor→UI (scenario 5): the toolkit marked its container surface with a cookie; attach the
    /// content host that minted it onto the host control rendering that surface. The entry is NOT removed here —
    /// the cookie stays live so a repeated mark of the SAME cookie (re-targeting a still-bound content host onto
    /// another surface) also resolves. It's dropped only when the content host detaches (which a toolkit-window
    /// teardown triggers, via OnTargetUnmapped → Detach) or swept once its weak target is collected.</summary>
    public static void OnContentSurfaceMarked(string cookie, uint hostId)
    {
        if (!s_contentCookies.TryGetValue(cookie, out var weak))
            return;
        if (!weak.TryGetTarget(out var contentHost))
        {
            // The content host was collected without an explicit Detach; its finalizer posts the compositor-side
            // unregister, so drop our stale entry and let the relationship fully sever.
            s_contentCookies.Remove(cookie);
            return;
        }
        if (s_hosts.TryGetValue(hostId, out var host))
            contentHost.ResolveTo(host);
    }

    // Drop entries whose content host was collected, so the static map can't accumulate dead weak refs over a long
    // run of content hosts that were dropped without an explicit Detach.
    private static void SweepDeadContentCookies()
    {
        List<string>? dead = null;
        foreach (var (cookie, weak) in s_contentCookies)
            if (!weak.TryGetTarget(out _))
                (dead ??= new List<string>()).Add(cookie);
        if (dead is not null)
            foreach (var cookie in dead)
                s_contentCookies.Remove(cookie);
    }

    /// <summary>The Avalonia Window to own a foreign-parented child: an exported Avalonia Window (scenario 3),
    /// else the parent toplevel's auto-window (scenario 4), else (scenario 1) the window hosting the parent's
    /// embedded control. Null when there is no owner.</summary>
    private static Window? ResolveOwnerWindow(uint parentHostId, string? parentWindowHandle)
    {
        if (parentWindowHandle is not null && s_exportedWindows.TryGetValue(parentWindowHandle, out var exported))
            return exported;
        if (parentHostId == 0)
            return null;
        if (s_autoWindows.TryGetValue(parentHostId, out var ownerWindow))
            return ownerWindow;
        if (s_hosts.TryGetValue(parentHostId, out var ownerHost))
            return TopLevel.GetTopLevel(ownerHost) as Window;
        return null;
    }

    // xdg_positioner anchor/gravity are discrete enums; Avalonia's are composable flags (TopLeft = Top|Left) with
    // different numeric values, so map by name rather than casting.
    private static PopupAnchor ToAvaloniaAnchor(XdgPositioner.AnchorEnum anchor) => anchor switch
    {
        XdgPositioner.AnchorEnum.Top => PopupAnchor.Top,
        XdgPositioner.AnchorEnum.Bottom => PopupAnchor.Bottom,
        XdgPositioner.AnchorEnum.Left => PopupAnchor.Left,
        XdgPositioner.AnchorEnum.Right => PopupAnchor.Right,
        XdgPositioner.AnchorEnum.TopLeft => PopupAnchor.TopLeft,
        XdgPositioner.AnchorEnum.TopRight => PopupAnchor.TopRight,
        XdgPositioner.AnchorEnum.BottomLeft => PopupAnchor.BottomLeft,
        XdgPositioner.AnchorEnum.BottomRight => PopupAnchor.BottomRight,
        _ => PopupAnchor.None,
    };

    private static PopupGravity ToAvaloniaGravity(XdgPositioner.GravityEnum gravity) => gravity switch
    {
        XdgPositioner.GravityEnum.Top => PopupGravity.Top,
        XdgPositioner.GravityEnum.Bottom => PopupGravity.Bottom,
        XdgPositioner.GravityEnum.Left => PopupGravity.Left,
        XdgPositioner.GravityEnum.Right => PopupGravity.Right,
        XdgPositioner.GravityEnum.TopLeft => PopupGravity.TopLeft,
        XdgPositioner.GravityEnum.TopRight => PopupGravity.TopRight,
        XdgPositioner.GravityEnum.BottomLeft => PopupGravity.BottomLeft,
        XdgPositioner.GravityEnum.BottomRight => PopupGravity.BottomRight,
        _ => PopupGravity.None,
    };

    /// <summary>Inspection hook for tests: the windows manufactured for auto-hosted toplevels (scenario 2).</summary>
    internal static IReadOnlyCollection<Window> AutoWindows => s_autoWindows.Values;

    /// <summary>Inspection hook for tests: the Avalonia popups currently open for embedded xdg_popups.</summary>
    internal static IReadOnlyCollection<Popup> Popups => s_popups.Values;
}
