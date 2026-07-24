using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWayland;
using NWayland.Protocols.Wayland;
using AvaloniaEmbedV1 = Avalonia.Wayland.Embedding.Protocol.AvaloniaEmbedV1;

namespace Avalonia.Wayland.Embedding.Hosting;

internal enum EmbedOutcome
{
    Bound,                 // the host resolved the token/cookie and wired the surface
    Rejected,              // the compositor rejected it (unknown token/cookie, wrong role, already mapped, …)
    EmbedderUnavailable,   // the avalonia_embedder global was never advertised on this connection
    ConnectionError,       // the toolkit's Wayland connection failed mid-handshake
}

/// <summary>
/// Toolkit-agnostic client-side glue for the <c>avalonia_embed</c> protocol, running on the TOOLKIT's own thread
/// over its OWN Wayland connection. The toolkit's <c>wl_display*</c> is adopted as a NON-OWNED NWayland client proxy
/// (cached per native handle so every call shares one coherent proxy map — NWayland keys native→managed routing on
/// the display handle). The <c>avalonia_embedder</c> global is bound exactly ONCE per connection
/// (<see cref="BindAndAssociate"/>, held by <see cref="WaylandEmbedderConnection"/>); <see cref="SendEmbedRequest"/>
/// then reuses that bound embedder and its private queue for each embed/mark, so dispatching never disturbs the
/// toolkit's default queue. (The display is taken explicitly rather than via <c>wl_proxy_get_display</c>, which
/// libwayland-client does not export; toolkits expose it directly, e.g. GTK's <c>gdk_wayland_display_get_wl_display</c>.)
/// </summary>
internal static class WaylandClientGlue
{
    // One borrowed WlDisplay per native wl_display*. NWayland keys its native→managed event routing on the
    // wl_display handle, so a second FromHandle for the same connection would overwrite the first wrapper and
    // strand the proxies (and pending result events) of in-flight calls. Cache to keep a single wrapper.
    private static readonly object s_lock = new();
    private static readonly Dictionary<IntPtr, WlDisplay> s_displays = new();

    /// <summary>
    /// Bind <c>avalonia_embedder</c> on a fresh private queue and associate the connection with <paramref name="ticket"/>
    /// (two round-trips: bind, then confirm the associate landed). Called ONCE at <c>WaylandEmbedderConnection</c>
    /// setup — outside any render, where a client round-trip is safe. Returns the adopted display + held queue +
    /// registry + embedder (the connection holds them for its lifetime), or null if the embedder global isn't
    /// advertised / the connection failed.
    /// </summary>
    public static (WlDisplay Display, WlEventQueue Queue, WlRegistry Registry, AvaloniaEmbedV1.AvaloniaEmbedder Embedder)? BindAndAssociate(IntPtr wlDisplayPtr, uint ticket)
    {
        if (wlDisplayPtr == IntPtr.Zero)
            return null;

        var display = AdoptDisplay(wlDisplayPtr);
        var queue = display.CreateEventQueue();

        AvaloniaEmbedV1.AvaloniaEmbedder? embedder = null;
        // The registry is RETURNED to the caller to hold: if it were dropped here, the GC finalizer thread would later
        // destroy this owned proxy on the toolkit's own wl_display, corrupting the shared connection.
        var registry = display.GetRegistry(new WlRegistry.Listener.Relay
        {
            OnGlobal = (reg, name, iface, version) =>
            {
                if (iface == "avalonia_embedder")
                    embedder = AvaloniaEmbedV1.AvaloniaEmbedder.Bind(reg, name, Math.Min(version, 1u), null, null);
            }
        }, queue);
        if (queue.Roundtrip() < 0 || embedder is null) // round-trip 1: receive globals + bind on our private queue
            return null;

        embedder.Associate(ticket);
        if (queue.Roundtrip() < 0) // round-trip 2: the compositor has recorded connection→ticket
            return null;
        return (display, queue, registry, embedder);
    }

    private static WlDisplay AdoptDisplay(IntPtr wlDisplayPtr)
    {
        lock (s_lock)
        {
            if (!s_displays.TryGetValue(wlDisplayPtr, out var display))
            {
                display = WlDisplay.FromHandle(wlDisplayPtr, ownsHandle: false, new WlDisplayOptions());
                s_displays[wlDisplayPtr] = display;
            }
            return display;
        }
    }

    /// <summary>
    /// Send one embed/mark request on an ALREADY-bound embedder (resolved once by <see cref="BindAndAssociate"/>) and
    /// pump its private queue for the one-shot bound/rejected result. No registry round-trip — the embedder global is
    /// never re-resolved. Called on the toolkit's thread, outside any render (a client round-trip is safe there).
    /// </summary>
    public static EmbedOutcome SendEmbedRequest(WlDisplay display, WlEventQueue queue,
        AvaloniaEmbedV1.AvaloniaEmbedder embedder, IntPtr wlSurfacePtr,
        Func<AvaloniaEmbedV1.AvaloniaEmbedder, WlSurface, WlEventQueue, AvaloniaEmbedV1.AvaloniaEmbedResult.Listener,
            AvaloniaEmbedV1.AvaloniaEmbedResult> request)
    {
        if (wlSurfacePtr == IntPtr.Zero)
            return EmbedOutcome.ConnectionError;

        // The surface is only a request argument (we never dispatch its events) → no queue/listener (NWayland
        // forbids a queue without a listener). Non-owned: we never destroy the toolkit's surface.
        var surface = WlSurface.Import(display, null, wlSurfacePtr, ownsHandle: false, null);

        var bound = false;
        var done = false;
        var result = request(embedder, surface, queue, new AvaloniaEmbedV1.AvaloniaEmbedResult.Listener.Relay
        {
            OnBound = _ => { bound = true; done = true; },
            OnRejected = _ => done = true,
        });
        // The compositor replies bound/rejected synchronously; one roundtrip suffices, but bound the wait and
        // surface a dead connection rather than spinning.
        for (var i = 0; i < 16 && !done; i++)
        {
            if (queue.Roundtrip() < 0)
                return EmbedOutcome.ConnectionError;
        }
        GC.KeepAlive(result);
        return !done ? EmbedOutcome.ConnectionError : bound ? EmbedOutcome.Bound : EmbedOutcome.Rejected;
    }
}
