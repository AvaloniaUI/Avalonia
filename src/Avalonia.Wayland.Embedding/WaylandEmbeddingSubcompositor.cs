using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Wayland.Embedding.Compositor;
using Avalonia.Wayland.Embedding.Hosting;

namespace Avalonia.Wayland.Embedding;

/// <summary>
/// Process-wide entry point for the in-process Wayland subcompositor that hosts other UI toolkits
/// (initially GTK3) inside Avalonia. The compositor runs on a dedicated background thread that never
/// shares mutable state with the UI thread; everything exposed here is UI-thread-affined and talks to
/// the compositor only through the message-queue / roundtrip machinery.
/// </summary>
/// <remarks>
/// There is no explicit lifecycle: the compositor thread auto-starts from the static constructor (it only
/// stands up an epoll loop + eventfd — nothing that can meaningfully fail) and is a background thread, so it
/// never blocks runtime teardown; a <see cref="AppDomain.ProcessExit"/> hook posts a shutdown sentinel.
/// </remarks>
public static class WaylandEmbeddingSubcompositor
{
    private static readonly ForwardingTracer s_tracer = new();
    private static readonly CompositorToUiChannel s_toUi;
    private static readonly WaylandCompositorWorker s_worker;
    private static readonly object s_traceLock = new();
    // volatile: read on the compositor thread in RaiseTrace, mutated under s_traceLock on subscriber threads.
    private static volatile Action<string>? s_protocolTrace;

    static WaylandEmbeddingSubcompositor()
    {
        s_toUi = new CompositorToUiChannel();
        s_toUi.RegisterRenderHook();
        s_worker = new WaylandCompositorWorker(s_toUi, s_tracer);
        AppDomain.CurrentDomain.ProcessExit += static (_, _) => s_worker.Shutdown();
    }

    /// <summary>Override the en-US xkb keymap stub (D1). Set before the first connection. (Honored in P2.)</summary>
    public static Func<string>? KeymapTextProvider { get; set; }

    /// <summary>
    /// Server-side protocol trace, formatted like <c>WAYLAND_DEBUG=1</c>. Per-message formatting only happens
    /// while there is at least one subscriber.
    /// </summary>
    public static event Action<string>? ProtocolTrace
    {
        add
        {
            lock (s_traceLock)
            {
                var wasEmpty = s_protocolTrace is null;
                s_protocolTrace += value;
                if (wasEmpty && s_protocolTrace is not null)
                    s_tracer.Enable(RaiseTrace); // only on the 0→1 transition
            }
        }
        remove
        {
            lock (s_traceLock)
            {
                s_protocolTrace -= value;
                if (s_protocolTrace is null)
                    s_tracer.Disable(); // only on the 1→0 transition
            }
        }
    }

    /// <summary>
    /// Create a socket pair and add the server end as a client. Returns the CLIENT fd to hand to the toolkit
    /// (via <c>WAYLAND_SOCKET</c>). Dispose the returned handle to drop the connection.
    /// </summary>
    public static (int ClientFd, IAsyncDisposable Connection) CreateConnection()
        => s_worker.CreateConnection();

    /// <summary>Adopt an existing connected socket fd. <paramref name="release"/> runs with the fd on disposal (default: close).</summary>
    public static IAsyncDisposable AddConnection(int fd, Action<int>? release = null)
        => s_worker.AddConnection(fd, release);

    /// <summary>Adopt an existing connected <see cref="Socket"/> (ownership transfers).</summary>
    public static IAsyncDisposable AddConnection(Socket socket)
        => s_worker.AddConnection(socket);

    /// <summary>
    /// Force the compositor to process all pending client requests and apply the results on the UI thread.
    /// Glue should call the toolkit's <c>wl_display_roundtrip()</c> first. Deadlock-free.
    /// </summary>
    public static void Roundtrip() => s_worker.Roundtrip();

    /// <summary>
    /// Scenario 4 (xdg-foreign in): resolve a handle a toolkit exported via <c>zxdg_exporter_v2</c> to the
    /// <see cref="WaylandSubcompositorControlHost"/> hosting that exported toplevel, so a dialog can be parented to
    /// the owning Avalonia Window (<c>TopLevel.GetTopLevel(host) as Window</c> — works however deeply the host is
    /// nested). Round-trips the compositor; returns null if the handle is unknown/revoked or its toplevel isn't
    /// mapped yet (no host). UI thread only.
    /// </summary>
    public static WaylandSubcompositorControlHost? ImportForeignXdgToplevel(string handle)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (string.IsNullOrEmpty(handle))
            return null;
        var hostId = WaitCompositorTaskWithRoundtrip(Api.ResolveForeignImportAsync(handle));
        return WaylandHosting.GetHostById(hostId);
    }

    /// <summary>
    /// Scenario 3 (xdg-foreign out): publish an Avalonia <see cref="Window"/> as a foreign handle so a toolkit can
    /// parent its dialog to it (<c>import_toplevel</c> + <c>set_parent_of</c>). Returns a
    /// <see cref="WaylandForeignExport"/> whose <c>Handle</c> you hand to the toolkit; dispose it (or close the
    /// window) to revoke. UI thread only.
    /// </summary>
    public static WaylandForeignExport ExportForeignXdgToplevel(Window window)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (window is null)
            throw new ArgumentNullException(nameof(window));
        var handle = "avln-window-" + Guid.NewGuid().ToString("N");
        WaylandHosting.RegisterExportedWindow(handle, window);
        Api.RegisterHostWindowExport(handle);
        Roundtrip(); // the handle is live compositor-side before the toolkit imports it
        return new WaylandForeignExport(handle, window);
    }

    // ── client-frame pump + synchronous resize flush ────────────────────────────────────────────────────────
    // All UI-thread state: RequestResizeFlush is called from a host's ArrangeOverride and RunResizeFlush from the
    // BeginInvokeOnRender callback, both on the UI thread, so no locking is needed.
    private static readonly List<Action> s_clientFramePumps = new();
    private static readonly HashSet<WaylandSubcompositorControlHost> s_pendingResizeHosts = new();
    private static bool s_resizeFlushScheduled;
    private static bool s_inResizeFlush;
    // The (connection ticket, surface object id) → new logical size of each toplevel that resized in the flush
    // currently invoking its pumps (null outside a flush). A WaylandEmbedderConnection matches its windows'
    // (ticket, wl_proxy_get_id) against this — no host refs, no round-trip (a client round-trip from inside the
    // render deadlocks on the toolkit's wl_display read lock; the ticket was associated once at connection setup, so
    // the object id is connection-scoped here). The size lets the glue apply the new bounds to its own widget tree.
    private static Dictionary<(uint Ticket, uint SurfaceId), (int Width, int Height)>? s_currentFlushResized;
    private static uint s_nextConnectionTicket = 1; // monotonic; never reused so a stale ticket can't false-match

    /// <summary>
    /// Register a callback that makes the embedded toolkit process its pending configure(s) and paint a fresh frame
    /// SYNCHRONOUSLY — without iterating the shared main loop (so nothing re-enters Avalonia / can't be frozen by a
    /// nested toolkit loop). For GTK the glue drives the widget's GdkFrameClock directly (queue_draw → display sync
    /// → emit layout/paint/after-paint). Invoked on the UI thread during the resize flush; several may be registered
    /// (multiple toolkits) and all run. With none registered the flush is skipped and a resize falls back to the
    /// stale buffer until the toolkit catches up on its own cadence (see
    /// <see cref="WaylandSubcompositorControlHost.StretchContent"/> for scaled-vs-1:1 during that window).
    /// </summary>
    public static void AddClientFramePumpCallback(Action pump)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (pump is not null && !s_clientFramePumps.Contains(pump))
            s_clientFramePumps.Add(pump);
    }

    /// <summary>Unregister a callback added via <see cref="AddClientFramePumpCallback"/>.</summary>
    public static void RemoveClientFramePumpCallback(Action pump)
    {
        Dispatcher.UIThread.VerifyAccess();
        s_clientFramePumps.Remove(pump);
    }

    // Callbacks run right before MediaContext commits the Avalonia compositors (after layout). The toolkit glue paints
    // its enqueued windows here so it never paints mid-layout; bound to MediaContext.BeforeCommitCompositors lazily.
    private static readonly List<Action> s_beforeCommitCallbacks = new();
    private static bool s_beforeCommitHooked;

    /// <summary>
    /// Register a callback invoked on the UI thread right before the Avalonia compositors are committed — after the
    /// layout pass and the resize flush. The toolkit glue emits its enqueued paint/after-paint HERE (not during the
    /// flush, which runs mid-layout), so the embedded toolkit's fresh buffer lands just before this frame is committed.
    /// </summary>
    public static void AddBeforeCommitCallback(Action callback)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (callback is null || s_beforeCommitCallbacks.Contains(callback))
            return;
        s_beforeCommitCallbacks.Add(callback);
        if (!s_beforeCommitHooked)
        {
            s_beforeCommitHooked = true;
            MediaContext.BeforeCommitCompositors += RunBeforeCommitCallbacks;
        }
    }

    /// <summary>Unregister a callback added via <see cref="AddBeforeCommitCallback"/>.</summary>
    public static void RemoveBeforeCommitCallback(Action callback)
    {
        Dispatcher.UIThread.VerifyAccess();
        s_beforeCommitCallbacks.Remove(callback);
    }

    private static void RunBeforeCommitCallbacks()
    {
        foreach (var callback in s_beforeCommitCallbacks)
        {
            try { callback(); }
            catch { /* a toolkit repaint throwing must not wedge the frame */ }
        }
    }

    /// <summary>True while a resize flush is invoking its pumps and at least one host resized — lets a pump skip its
    /// (potentially expensive) enumeration when there is nothing to repaint.</summary>
    public static bool HasPendingResizes => s_currentFlushResized is { Count: > 0 };

    /// <summary>
    /// Create a per-<c>wl_display</c> connection the toolkit glue owns: it binds <c>avalonia_embedder</c> on a private
    /// queue and associates this connection with a fresh ticket (the binding + associate round-trips happen HERE, at
    /// glue-controlled time — call it OUTSIDE any render). The returned object then answers
    /// <see cref="WaylandEmbedderConnection.QueryResizedSurfaces"/> during a resize flush with no further round-trip.
    /// Dispose it when the toolkit connection goes away. UI thread only.
    /// </summary>
    public static WaylandEmbedderConnection CreateEmbedderConnection(IntPtr wlDisplay)
    {
        Dispatcher.UIThread.VerifyAccess();
        var ticket = s_nextConnectionTicket++;
        return WaylandEmbedderConnection.Create(wlDisplay, ticket);
    }

    /// <summary>UI thread: the new logical size of the surface with object id <paramref name="surfaceObjectId"/> on
    /// the connection tagged <paramref name="ticket"/> if it resized in the current flush, else null. Pure in-memory;
    /// null outside a flush.</summary>
    internal static (int Width, int Height)? ResizedSurfaceSize(uint ticket, uint surfaceObjectId)
        => surfaceObjectId != 0 && s_currentFlushResized is { } resized
            && resized.TryGetValue((ticket, surfaceObjectId), out var size) ? size : null;

    /// <summary>UI thread: a host's allocation changed (it has posted a configure); schedule a single coalesced
    /// resize flush so the toolkit's new-size frame is pulled in before this Avalonia frame draws.</summary>
    internal static void RequestResizeFlush(WaylandSubcompositorControlHost host)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (s_clientFramePumps.Count == 0)
            return; // nothing to pump with ⇒ stale-buffer fallback; don't accumulate work that can't be flushed
        s_pendingResizeHosts.Add(host);
        ArmResizeFlush();
    }

    // Register the (single) flush callback, unless one is already pending. Idempotent and re-entrancy-safe — the
    // pending-set guard in RunResizeFlush means a redundant registration is a harmless no-op.
    private static void ArmResizeFlush()
    {
        if (s_resizeFlushScheduled || s_clientFramePumps.Count == 0)
            return;
        s_resizeFlushScheduled = true;
        MediaContext.Instance.BeginInvokeOnRender(RunResizeFlush);
    }

    // Pull the resized clients' new-size frames synchronously, once, after layout and before the visual draws.
    private static void RunResizeFlush()
    {
        s_resizeFlushScheduled = false; // this registration is being consumed
        if (s_inResizeFlush)
        {
            // Re-entered: the live GLib pump dispatched a nested render. The outer flush owns this frame's work;
            // re-arm so anything queued during it still flushes, and don't double-tick the toolkit.
            ArmResizeFlush();
            return;
        }
        if (s_pendingResizeHosts.Count == 0)
            return;
        s_inResizeFlush = true;
        try
        {
            // Snapshot + clear first, so a resize that arrives DURING the pump accumulates for the next flush.
            var hosts = new WaylandSubcompositorControlHost[s_pendingResizeHosts.Count];
            s_pendingResizeHosts.CopyTo(hosts);
            s_pendingResizeHosts.Clear();
            // 1) Thaw each resized surface's frame clock — fire the present-pace callbacks we were holding so the
            //    client may paint its configure-driven frame now rather than only after the next present. Record each
            //    resized (connection ticket, surface id) → new logical size so the glue can repaint + resize the
            //    right windows.
            var resized = new Dictionary<(uint, uint), (int, int)>();
            foreach (var host in hosts)
            {
                host.ReleaseDeferredFrames();
                if (host.SurfaceObjectId != 0 && host.ArrangedWidth > 0 && host.ArrangedHeight > 0)
                    resized[(host.ConnectionTicket, host.SurfaceObjectId)] = (host.ArrangedWidth, host.ArrangedHeight);
            }
            s_currentFlushResized = resized;
            Roundtrip();                              // 2) deliver the configures + released callbacks to the clients
            foreach (var pump in s_clientFramePumps)  // 3) tick the toolkit once → it relayouts + commits a new buffer
            {
                try { pump(); }
                catch { /* a toolkit pump throwing must not wedge the frame */ }
            }
            Roundtrip();                              // 4) capture the fresh buffer (applied UI-side) for this draw
        }
        finally
        {
            s_currentFlushResized = null;
            s_inResizeFlush = false;
            if (s_pendingResizeHosts.Count > 0)
                ArmResizeFlush(); // work queued during the pump — flush it next frame
        }
    }

    /// <summary>
    /// UI→compositor cross-thread proxy. Every call is marshalled onto the compositor worker thread; safe to call
    /// from any thread (the marshaller posts through a thread-safe queue). See <see cref="IWaylandEmbedderApi"/>.
    /// </summary>
    internal static WaylandEmbedderApiProxy Api => s_worker.Api;

    /// <summary>
    /// Drive a request/response proxy call (a <c>Task&lt;T&gt;</c> from an echo-back <c>Api</c> method) to completion
    /// and return its result. The proxy posted the work onto the compositor queue ahead of the roundtrip's drain
    /// sentinel, so once <see cref="Roundtrip"/> returns the worker has already produced the value — the task is
    /// complete and reading it can't block. UI thread only.
    /// </summary>
    internal static T WaitCompositorTaskWithRoundtrip<T>(Task<T> task)
    {
        Dispatcher.UIThread.VerifyAccess();
        Roundtrip();
        return task.GetAwaiter().GetResult();
    }

    /// <summary>Register a scenario-1 embedding token and return the host id the compositor bound it to.</summary>
    internal static uint RegisterEmbedToken(string token)
        => WaitCompositorTaskWithRoundtrip(Api.RegisterEmbedTokenAsync(token));

    internal static void RaiseTrace(string message) => s_protocolTrace?.Invoke(message);
}
