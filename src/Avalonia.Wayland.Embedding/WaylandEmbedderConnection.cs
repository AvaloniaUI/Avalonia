using System;
using Avalonia.Threading;
using Avalonia.Wayland.Embedding.Hosting;
using NWayland.Protocols.Wayland;
using AvaloniaEmbedV1 = Avalonia.Wayland.Embedding.Protocol.AvaloniaEmbedV1;

namespace Avalonia.Wayland.Embedding;

/// <summary>
/// A per-<c>wl_display</c> handle the toolkit glue creates (once) and owns, returned by
/// <see cref="WaylandEmbeddingSubcompositor.CreateEmbedderConnection"/>. Creating it binds <c>avalonia_embedder</c>
/// on a private event queue and associates the connection with a process-unique ticket — the round-trips that
/// requires happen at construction (glue-controlled time, OUTSIDE any render, where a client round-trip is safe).
///
/// Thereafter <see cref="QueryResizedSurfaces"/> answers from in-memory state with NO round-trip, so it is safe to
/// call from inside the resize flush (a client <c>wl_display</c> round-trip there deadlocks on the toolkit's own
/// read lock). The ticket scopes a surface's wayland object id to THIS connection (object ids are only unique per
/// connection), so matching by <c>wl_proxy_get_id</c> can't collide across connections.
/// </summary>
public sealed class WaylandEmbedderConnection : IDisposable
{
    private readonly uint _ticket;
    // The non-owned, per-handle-cached toolkit display: needed to import surfaces as embed/mark request arguments.
    private WlDisplay? _display;
    // Held (not just for disposal) so the GC finalizer thread never destroys these owned proxies on the toolkit's
    // own wl_display — which would corrupt the shared connection.
    private WlEventQueue? _queue;
    private WlRegistry? _registry;
    private AvaloniaEmbedV1.AvaloniaEmbedder? _embedder;
    private bool _disposed;

    private WaylandEmbedderConnection(uint ticket, WlDisplay display, WlEventQueue queue, WlRegistry registry, AvaloniaEmbedV1.AvaloniaEmbedder embedder)
    {
        _ticket = ticket;
        _display = display;
        _queue = queue;
        _registry = registry;
        _embedder = embedder;
    }

    internal static WaylandEmbedderConnection Create(IntPtr wlDisplay, uint ticket)
    {
        var bound = WaylandClientGlue.BindAndAssociate(wlDisplay, ticket)
            ?? throw new InvalidOperationException("avalonia_embedder is not available on this wl_display.");
        return new WaylandEmbedderConnection(ticket, bound.Display, bound.Queue, bound.Registry, bound.Embedder);
    }

    /// <summary>Scenario 1: emit <c>embed_toplevel(surface, token)</c> on this connection's single bound embedder so
    /// the toolkit toplevel renders into the host that minted <paramref name="token"/>. UI thread only.</summary>
    internal EmbedOutcome EmbedToplevel(IntPtr wlSurfacePtr, string token)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (_display is null || _queue is null || _embedder is null)
            return EmbedOutcome.EmbedderUnavailable;
        return WaylandClientGlue.SendEmbedRequest(_display, _queue, _embedder, wlSurfacePtr,
            (embedder, surface, queue, onResult) => embedder.EmbedToplevel(surface, token, onResult, queue));
    }

    /// <summary>Scenario 5: emit <c>mark_content_surface(surface, cookie)</c> on this connection's single bound
    /// embedder, tagging the toolkit window's surface as an Avalonia-content container. UI thread only.</summary>
    internal EmbedOutcome MarkContentSurface(IntPtr wlSurfacePtr, string cookie)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (_display is null || _queue is null || _embedder is null)
            return EmbedOutcome.EmbedderUnavailable;
        return WaylandClientGlue.SendEmbedRequest(_display, _queue, _embedder, wlSurfacePtr,
            (embedder, surface, queue, onResult) => embedder.MarkContentSurface(surface, cookie, onResult, queue));
    }

    /// <summary>
    /// During a resize flush, report for each surface wayland object id (the toolkit's <c>wl_proxy_get_id</c> of a
    /// window's surface, on THIS connection) the host's new logical size if it resized in the current flush, else
    /// null. Pure in-memory <c>(ticket, id)</c> lookup: no round-trip, no host references. All null outside a flush.
    /// The glue applies the returned size to its window so the in-flush frame is the right size; a matching
    /// <c>xdg_toplevel.configure</c> is also sent so the toolkit's own state stays coherent once it resumes. UI thread only.
    /// </summary>
    public (int Width, int Height)?[] QueryResizedSurfaces(uint[] surfaceObjectIds)
    {
        Dispatcher.UIThread.VerifyAccess();
        var result = new (int Width, int Height)?[surfaceObjectIds.Length];
        for (var i = 0; i < surfaceObjectIds.Length; i++)
            result[i] = WaylandEmbeddingSubcompositor.ResizedSurfaceSize(_ticket, surfaceObjectIds[i]);
        return result;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _embedder?.Dispose();
        _registry?.Dispose();
        _queue?.Dispose();
        _embedder = null;
        _registry = null;
        _queue = null;
        _display = null; // non-owned + cached per handle elsewhere — release the ref, don't destroy it
    }
}
