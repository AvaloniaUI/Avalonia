# Wayland embedding subcompositor

`Avalonia.Wayland.Embedding` hosts widgets and windows written in **other UI toolkits** (GTK3 today) *inside*
an Avalonia application, by running an **in-process Wayland compositor**. The foreign toolkit connects to us
over a socketpair (`WAYLAND_SOCKET`), commits `wl_shm` buffers as if to a real display, and those buffers are
drawn by ordinary Avalonia controls. It is `wl_shm`-only — no dmabuf/GPU buffers.

This is the mirror image of the sibling `Avalonia.Wayland` platform. **That** project makes Avalonia a Wayland
*client*; **this** one makes Avalonia a Wayland *server / compositor* (it uses the `.Server` side of NWayland).
The two also have **opposite threading models** — see below — so do not carry intuitions across.

`Avalonia.Wayland.Embedding.Gtk` is an **example** GTK/GtkSharp glue that drives this compositor; it is not
shipped on NuGet. A real consumer writes equivalent glue for its own toolkit against the public API here.

## Where the protocol details live

Do not re-derive Wayland behavior. For NWayland binding conventions (snake_case→CamelCase, listeners passed at
bind time, `Is<Name>Available` version gates, where to find protocol `.xml`/docs, the "always pin the supported
version range" rule) see **`../Avalonia.Wayland/README.md`** and the NWayland-generated XML docs — all of it
applies here too and is not repeated. The one addition is that we consume the **`.Server`** bindings
(`NWayland.Server`, plus `NWayland.Protocols.Plasma` for `org_kde_kwin_server_decoration`), and our one private
protocol `avalonia_embed_v1` is generated **internal** from `Protocols/avalonia_embed_v1.xml`.

## Goals

- Present a foreign toolkit's toplevel as an Avalonia control (a pre-created host that mints a **token**), or
  auto-host a plain toplevel into a manufactured Avalonia `Window` (act as a rootless compositor: proxy
  title / min-max / state / activation / close to the real `Window`).
- Parent dialogs across the boundary in both directions (xdg-foreign in/out).
- Nest Avalonia content back *inside* a hosted toolkit window.
- Keep embedded content **frame-perfect**: the toolkit is throttled to Avalonia's present cadence and resizes
  are pulled in-frame, so embedded pixels never tear or lag against the surrounding Avalonia UI.

The concrete request/response flows for each scenario live alongside the source (see *Further reading*); this
document is about the **goals, invariants, and non-functional requirements** the implementation must uphold.

## Non-functional requirements

These are load-bearing. The code is full of small, non-obvious workarounds that exist **only** to satisfy them;
before "simplifying" anything, confirm it against the invariant it protects (and `caveats.md`).

### Strict cross-thread isolation

There are exactly two threads and **no shared mutable domain state** between them:

- the **compositor worker thread** — owns the `WaylandServer`, every `*.Server` resource, all listeners, and
  all per-client / per-surface / role state;
- the **UI thread** — owns every `Control` / `Window` / `Popup` / `Bitmap` and all layout.

They communicate **only** through message queues + wakeups. Cross-thread references are always by **opaque id**
(a surface id, host id, connection ticket) plus value data — never a live object. See *Threading rules*.

### The compositor never blocks on the UI thread

The worker thread's **only** park point is `epoll_wait`. Every protocol obligation — configure, `ack_configure`,
`wl_display_roundtrip`/`sync`, buffer release, pings, embedding requests — is answered **inline** on the worker
thread without ever waiting on the UI thread. `wl_surface.frame` callbacks are the *only* events intentionally
delayed, and they are delayed by **not sending them yet**, not by blocking. Consequently a misbehaving or busy
UI thread can never wedge a connected client, and a client can never wedge the compositor.

### Deadlock-free round-tripping

Both directions of "make the other side catch up, then continue" are deadlock-free:

- **UI → compositor:** `WaylandEmbeddingSubcompositor.Roundtrip()` posts a drain sentinel and then *pumps its
  own inbound queue while waiting* (the classic pump-while-waiting pattern). Because the compositor never waits
  on the UI thread, the sentinel is always eventually processed; because the waiter keeps draining, it can never
  starve the compositor. Safe to call from any thread.
- **GOLDEN RULE — never round-trip the *toolkit's* `wl_display` from inside a render / the resize flush.** The
  flush runs inside `MediaContext.RenderCore`; a client round-trip there (anything reaching
  `wl_display_read_events`, incl. `gdk_display_sync`) **deadlocks** on the toolkit's own read lock. Inside a
  render do only **write-only** client work (`wl_display_flush`); use the *worker* `Roundtrip()` (which never
  touches the toolkit's read path) instead. Per-frame surface→host resolution is **in-memory**, never a
  protocol query. This one is easy to reintroduce by accident — `caveats.md` documents it at length.

### Present-paced embedding

The embedded toolkit is pinned to Avalonia's frame cadence: the compositor **defers** each surface's frame
callbacks and fires them only after the host control renders (`Control.Render` is the next-frame signal — no
separate liveness timer). An occluded / unselected-tab host simply withholds callbacks, exactly as a real
compositor treats a non-visible surface, and the client pauses and resumes on its own.

### UI-thread-affined public API, no lifecycle

Everything public is **UI-thread-affined** unless explicitly noted; all cross-thread work is hidden behind the
queue/roundtrip machinery. There is **no `Initialize`/`Shutdown`**: the compositor thread auto-starts from a
static constructor (it only stands up an epoll loop + eventfd — nothing that can meaningfully fail), runs as a
background thread, and a `ProcessExit` hook posts a shutdown sentinel.

## Threading rules

**NO CROSS-THREAD VARIABLE ACCESS. MESSAGING ONLY.**

- Compositor interactions run on the dedicated **worker thread**. Wayland `*.Server` resources, listeners, and
  compositor state (`CompositorState`, `ClientContext`, `SurfaceState`, `XdgShellState`, …) are touched **only**
  there.
- UI-thread objects (controls, windows, bitmaps) are touched **only** on the UI thread.
- Neither side reads the other's fields. No `volatile`, no locks, no "safe" shared state — the message queues
  are the only synchronized thing, precisely because there is nothing else to synchronize.
- **UI → compositor** goes through the generated `IWaylandEmbedderApi` proxy, which marshals every call onto the
  worker queue; the worker's non-blocking drain processes those posted jobs alongside network events on the same
  loop, so a UI job is never serviced by blocking the compositor.
- **Compositor → UI** enqueues onto `CompositorToUiChannel`; the UI thread applies it in `Drain`, invoked from
  three triggers: the `BeforeRender` dispatcher post, the `MediaContext.RenderCore` pre-pulse hook (so this
  frame's layout sees the latest committed state), and the synchronous roundtrip pump. One drain implementation,
  three callers.
- UI→compositor work *produced during* the layout cycle (a host's arranged size → `xdg_toplevel.configure`;
  scenario-5 rect/clip updates; the resize flush) is coalesced onto a one-shot `BeginInvokeOnRender` callback so
  it is emitted at one consistent point, never mid-measure.
- Server objects may have `Post`-ed work and constructor arguments handed in from the UI thread; their internal
  state is otherwise never modified from UI-thread code.

## Architecture at a glance

- **`WaylandEmbeddingSubcompositor`** — the process-wide static entry point: attach connections
  (`CreateConnection`/`AddConnection`), the deadlock-free `Roundtrip`, xdg-foreign export/import, the
  client-frame-pump + resize-flush machinery, and `ProtocolTrace` (a `WAYLAND_DEBUG`-style server trace).
- **`WaylandCompositorWorker`** — owns the `WaylandServer` and the non-blocking epoll dispatch loop
  (`NextEventPending` → `CompleteSatisfiedRoundtrips` → park in `NextEvent`). Implements `IWaylandEmbedderApi`.
- **Compositor state** — `ClientContext` advertises the globals (`wl_compositor` v6 for HiDPI, `wl_shm`,
  `wl_subcompositor`, `wl_seat`, `xdg_wm_base`, xdg-foreign **v1 and v2** (GTK3 only binds v1), text-input-v3,
  server-side-decoration, and `avalonia_embedder`); `SurfaceState`/`XdgShellState`/`ShmState` model the
  double-buffered surface + roles; input, xkb keymap, and HiDPI (`preferred_buffer_scale`) round it out.
- **Hosting (UI side)** — `WaylandSubcompositorControlHost` draws one embedded surface tree and posts rendered
  ids back to release frame callbacks; `WaylandHosting` manufactures auto-windows/popups;
  `WaylandSubcompositorAvaloniaContentHost` inserts Avalonia content *into* a hosted toolkit window.
- **Client glue** — `WaylandClientGlue` (toolkit-agnostic) speaks `avalonia_embed` over the toolkit's *own*
  connection on a private event queue; `WaylandEmbedderConnection` binds the embedder once and carries a
  process-unique **ticket** so per-frame surface matching (`wl_proxy_get_id`) is connection-scoped and never
  needs a round-trip.

### Custom protocol `avalonia_embed_v1`

A private, `internal`-generated protocol whose only job is to carry an out-of-band identifier so the compositor
can bind a client object to a specific Avalonia host: `associate(ticket)` scopes a connection,
`embed_toplevel(surface, token)` adopts a toolkit toplevel into the host that minted the token, and
`mark_content_surface(surface, cookie)` binds Avalonia content into a hosted window's surface. Tokens/cookies
are minted UI-side and round-tripped live before they are handed out, so a missing one is an unambiguous error.

## Scope / non-goals

`wl_shm` only (no dmabuf/GPU, no zero-copy yet); one virtual `wl_output`; SSD forced via
`org_kde_kwin_server_decoration` (no `zxdg_decoration_manager_v1`); stub `wl_data_device_manager` (no
clipboard/DnD); no fractional-scale-v1, multi-seat, or touch.

## Further reading

- **`caveats.md`** (this directory) — the toolkit-agnostic hard-won findings: the toolkit-`wl_display` deadlock,
  the resize-flush split, `wl_proxy_get_id` connection scoping. Read it before touching the resize or client-glue
  paths.
- **`../Avalonia.Wayland.Embedding.Gtk/caveats.md`** — GTK/GLib/GtkSharp-specific findings (the GLib deadlock
  mechanism, frame-clock driving, GtkSharp toggle-ref quirk, real-GTK test timing).
- **`../Avalonia.Wayland/README.md`** — NWayland conventions, protocol-doc locations, global/versioning rules.
