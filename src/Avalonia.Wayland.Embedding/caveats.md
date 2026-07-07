# Caveats & hard-won findings (toolkit-agnostic)

Non-obvious things that will bite you when working on `Avalonia.Wayland.Embedding`. Each entry is something that
cost real debugging time; keep them in mind before "simplifying" the code that works around them. Everything
here applies to **any** embedded toolkit. Toolkit-specific findings (GLib/GDK/GtkSharp) live next to the glue —
see `../Avalonia.Wayland.Embedding.Gtk/caveats.md`.

## Threading & the toolkit's `wl_display`

The glue talks to the in-process compositor over the **toolkit's own** `wl_display` (obtained from the toolkit,
e.g. GTK's `gdk_wayland_display_get_wl_display`). That connection is shared with, and driven by, the toolkit's
own main loop. Several traps follow from that.

### Never roundtrip the toolkit's `wl_display` from inside a render / the resize flush

The resize flush runs inside `MediaContext.RenderCore` (registered via `MediaContext.BeginInvokeOnRender`). A
**client** roundtrip there — `wl_display_roundtrip[_queue]`, or anything that ends in `wl_display_read_events`
(e.g. a toolkit "sync") — can **deadlock**: the toolkit's own main loop may already hold libwayland's
`wl_display_prepare_read` lock while our high-priority render runs, so our `wl_display_read_events` blocks
forever waiting for a reader that never gets to call `read`/`cancel_read`.

This is **not** a libwayland bug and **not** a reader-coordination deadlock (`prepare_read` succeeds, the queue is
empty). `wl_display_read_events` blocking means *we already hold the read intent* and no data is coming. The exact
mechanism for a GLib-based toolkit (GSource priority inversion) is documented in the Gtk `caveats.md`; the rule
below holds regardless of toolkit.

**Rules:**
- Inside the flush, do client-side work that is **write-only**: `wl_display_flush` is fine; roundtrips/sync are not.
- The compositor-side `WaylandEmbeddingSubcompositor.Roundtrip()` (the *worker* roundtrip) is safe from anywhere —
  it parks on the in-process `CompositorToUiChannel` signal and never touches the toolkit's `wl_display` read path.
- Per-frame surface→host resolution must be **in-memory**, not a protocol query (see "Resize" below).

### Owned NWayland proxies must be kept alive — the finalizer thread will destroy them on the toolkit's display

NWayland client proxies own their `wl_proxy`. If you create a `wl_registry` / queue / bound global and drop the
reference, the **GC finalizer thread** later calls `wl_proxy_destroy` on the **toolkit's** `wl_display`,
concurrently with the toolkit using it — corrupting the shared connection. `WaylandEmbedderConnection` holds the
queue, registry, and embedder for its whole lifetime for exactly this reason (not just for `Dispose`). Don't
"tidy" them into locals.

## Resize / frame-perfect embedding

The embedded toolkit is throttled to Avalonia's frame cadence: the compositor defers each surface's
`wl_surface.frame` callbacks and fires them only after the host control renders (`IWaylandEmbedderApi.FireFrameCallbacks`,
posted from `ReleaseDeferredFrames`). So the toolkit cannot paint frame N+1 until Avalonia has presented frame N.
During a resize this produces a stale frame unless we synchronously pull the new one — the "wiggle".

### The synchronous resize flush (split: size in-layout, paint before-commit)

Resizing is split across two UI-thread phases so the toolkit never paints during Avalonia's layout pass (painting
mid-layout re-enters the dispatcher). Both phases drive the toolkit **synchronously, without iterating the shared
main loop** — iterating it would re-enter Avalonia's dispatcher (nested render) or let a nested toolkit loop
freeze us.

**1. Resize flush** — `WaylandSubcompositorControlHost.ArrangeOverride` posts the configure and calls
`RequestResizeFlush`, which coalesces into one `BeginInvokeOnRender` callback per frame (many hosts folded in). It:

1. Releases the resized surfaces' deferred frame callbacks (thaw the toolkit's frame clock).
2. Worker-roundtrips (deliver configures / released callbacks).
3. Invokes each registered **client-frame pump** once. A pump is the glue's toolkit-specific hook (registered via
   `AddClientFramePumpCallback`): it may size the resized windows and enqueue their frames, but must **not** paint
   here (see phase 2) and must **not** roundtrip the toolkit's display.
4. Worker-roundtrips again.

**2. Before-commit repaint** — a callback registered on `MediaContext.BeforeCommitCompositors` (via
`AddBeforeCommitCallback`) runs after layout and just before the Avalonia compositors commit. The glue paints its
enqueued windows here, then does a `wl_display` **flush** (write-only — never a roundtrip/sync from a render) + a
**worker** roundtrip, so the toolkit's fresh buffer is captured into this same commit.

### `wl_proxy_get_id` matching needs a connection ticket — never raw object ids

To repaint only the resized widgets, the glue must map its windows back to the resized hosts. It cannot roundtrip
(above), so it matches by the surface's wayland object id (`wl_proxy_get_id` client-side ==
`WlSurface.Server.ObjectId` server-side, equal by the `new_id` mechanism). But **raw object ids are unsafe to
match on**: they are unique only *per connection* and are *reused* after destroy. A stale/colliding id can
false-match.

Fix: `WaylandEmbedderConnection` associates the connection with a process-monotonic, never-reused **ticket** (the
`associate` request, server-side, so the compositor does the connection scoping — the glue never needs connection
info). Each toplevel's map carries `(ConnectionTicket, SurfaceObjectId)`; the flush matches
`(ticket, wl_proxy_get_id(surface))`. Pure in-memory, connection-scoped, collision-free.

### One-frame lag → glue-applied in-flush sizing

Flush-only (no toolkit-display read in the flush) means the toolkit reads the new `xdg_toplevel.configure` on its
*next* loop turn, so left alone it paints one frame behind. To get same-frame sizing, the glue pulls the new logical
size from `WaylandEmbedderConnection.QueryResizedSurfaces` (returned in-memory, no roundtrip) and applies it to its
widget tree **itself**, inside the pump.

The compositor **still sends the matching sized configure** — it is required, not redundant. When the toolkit later
resumes its loop and processes that configure, the size it carries keeps the toolkit's own window state coherent;
withhold it and the toolkit resets the surface back to its own size on the next turn. The glue's in-flush size and
the configured size are the same value, so when the toolkit finally acks the configure it is a no-op re-allocation.
(Applying the size in-flush is toolkit-specific — see the Gtk `caveats.md`.)

`StretchContent` (per host, default true) is the fallback when no pump catches up in time: true scales the stale
buffer (the old wiggle); false draws 1:1 top-left clipped (a one-frame edge gap instead of scale distortion).
Auto-windows and frame-perfect wrappers set it false.

## Protocol / compositor invariants

### Embedding requires the toolkit's `xdg_toplevel` role to exist first

`embed_toplevel` / `mark_content_surface` both resolve the toplevel role from the surface server-side and are
**rejected** if it has none. So the glue must have created the role (shown/realized the window to the point gdk
et al. issue `get_toplevel` + the role-setup commit) **before** sending the embed request — but **before the
window maps** (a mapped toplevel can't be embedded). The embed request is sent on a private event queue precisely
so the toolkit's default-queue events don't dispatch (and map the window) mid-embed. The toolkit-specific timing
of when the role appears is in the Gtk `caveats.md`.

### Host ids and connection tickets are monotonic and never reused

`HostId` (assigned at map) and the connection ticket are monotonic and never recycled. Several invariants lean on
this (e.g. a late `popup_done` for a since-gone host can't hit a different one; a resized-set membership check by
ticket can't alias a different connection). Don't switch to recycled handles.

## Tests

There are two test layers. The **NWayland-client** tests (`WaylandTestClient`) simulate the toolkit side over the
same compositor with the NWayland client bindings — deterministic and free of any real toolkit's main-loop
timing; **prefer them**. The **real-toolkit** integration tests exercise the actual glue path and are inherently
timing-sensitive (wait on post-conditions, not fixed pump counts); their caveats live next to the glue, in
`../Avalonia.Wayland.Embedding.Gtk/caveats.md`.
