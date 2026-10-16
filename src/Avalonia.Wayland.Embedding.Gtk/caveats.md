# Caveats & hard-won findings (GTK / GLib / GtkSharp)

Toolkit-specific findings for the GTK3 glue (`Avalonia.Wayland.Embedding.Gtk`). These are GDK/GLib/GtkSharp
behaviors — a glue for a different toolkit would have its own equivalents. The toolkit-agnostic invariants they
build on live in `../Avalonia.Wayland.Embedding/caveats.md`; read that first.

## Why a client roundtrip from the render deadlocks (the GLib mechanism)

The core rule "never roundtrip the toolkit's `wl_display` from inside a render / the resize flush" has a concrete
cause under GTK's GLib main loop:

- The flush runs inside `MediaContext.RenderCore`, which under `UseGLibMainLoop` is a GLib source dispatch.
- GLib does **not** reliably call `check` on a prepared GSource before dispatching a *different, higher-priority*
  source. Avalonia's GLib dispatcher registers its "signaled" source at `G_PRIORITY_DEFAULT - 1`
  (`GlibDispatcherImplBase`), i.e. **above** GTK's wayland source.
- So GLib can dispatch our render (the high-priority Avalonia job) **while GTK's wayland GSource still holds its
  `wl_display_prepare_read` lock**. Our `wl_display_read_events` then blocks forever waiting for a reader (GTK)
  that never gets to call `read`/`cancel_read`.
- Symptom: the UI thread is parked in `wl_display_read_events`; the compositor thread sits idle in `poll`, having
  sent nothing — it looks like "stuck on the initial registry roundtrip", but it is the read-lock wait.

Do only **write-only** client work in the flush (`gdk_display_flush` / `wl_display_flush`); use the worker
`WaylandEmbeddingSubcompositor.Roundtrip()` for anything that needs the compositor to catch up.

## The `xdg_toplevel` role is created at `ShowAll`, not `Realize`

`gtk_widget_realize` only creates the `wl_surface`. gdk creates the `xdg_toplevel` role (and the role-setup
commit) at `ShowAll`. So the glue must `ShowAll` + flush **before** `embed_toplevel` / `mark_content_surface`
(both require a toplevel role server-side, else they're rejected). The private event queue used by the embed
request keeps gdk's default-queue events from dispatching during the embed, so the window can't map first.

## In-flush sizing on GTK is not a one-liner

The glue applies the resized logical size (from `QueryResizedSurfaces`) to the widget tree itself, in the pump
(`DriveEmbeddedFrames`). `Gdk.Window.Resize` (gdk_window_resize) alone leaves the widget tree at the old size:
GTK *withholds* `gtk_widget_size_allocate` until it sees the size it queued confirmed by the compositor — a
confirmation we can't deliver in-flush, because a `wl_display` roundtrip from the flush deadlocks (above). The
glue sidesteps that by calling `Gtk.Widget.SizeAllocate` **directly** at the new bounds, right after the resize —
forcing the allocation in-line. The clock is then enqueued; the actual `paint`/`after-paint` runs later, in the
before-commit repaint.

An earlier approach fed a synthetic `GDK_CONFIGURE` (a hand-built event through `gtk_main_do_event`) between two
frame-clock `layout` passes to unblock GTK's own `size_allocate`; it was replaced by the direct `SizeAllocate`
call above and removed.

## Driving the GdkFrameClock synchronously (frame-clock vtable patch)

The before-commit repaint drives each enqueued window's `GdkFrameClock` by hand — `g_signal_emit_by_name(clock,
"paint")` then `"after-paint"` — instead of iterating the GLib loop. GDK's after-paint commits a buffer and
**freezes** the clock to await a `wl_surface.frame` callback we deliver out-of-band, so on every forced frame the
freeze count would climb and the clock would stall. `FrameClockHelper` patches the `GdkFrameClockClass` vtable
(freeze/thaw/dispose/constructed) to track each clock's freeze state and neutralize only the **redundant**
after-paint freeze, leaving the single legit throttle freeze for the real frame callback to clear. The patch is
per-class (all `GdkFrameClockIdle` instances share one vtable), done once at glue init against a pinned class.

## GTK3 binds xdg-foreign **v1**, not v2

GTK 3.24.x has no `zxdg_exporter_v2`/`zxdg_importer_v2` support at all — its registry handler string-matches
`zxdg_exporter_v1` exactly. If the compositor advertises only v2, gdk leaves `xdg_exporter` NULL and
`gdk_wayland_window_export_handle` / `set_transient_for_exported` fail. The compositor therefore advertises
**both** v1 and v2 (the v1 listeners are thin shells over the same `ForeignSupport` helpers as v2; v1 names the
requests `export`/`import` vs v2's `export_toplevel`/`import_toplevel`). The v1 bindings ship in NWayland.dll — no
extra protocol XML.

## GtkSharp consumers must dispose widgets in their `Destroyed` handler (works around a GtkSharp toggle-ref bug)

Not a compositor bug — a **GtkSharp** defect a consumer hits when it destroys widgets. `gtk_widget_destroy` emits
`destroy`; GtkSharp's marshaller re-wraps the sender (`GLib.Object.GetObject` misses the already-dict-evicted
wrapper) and adds a **second** `g_object_add_toggle_ref`, so GLib aborts in `toggle_refs_notify`
(`assertion n_toggle_refs == 1`; SIGABRT under create/destroy churn). Workaround (NOT "correct usage"):
`widget.Destroyed += (s, _) => ((Gtk.Widget)s).Dispose();` — disposes the duplicate wrapper in time. See
<https://github.com/GtkSharp/GtkSharp/issues/248#issuecomment-941704980>. Covered by
`GtkToggleRefChurnTests.Auto_host_churn_100x`.

## Tests

### The manual test pump must give GDK's frame clock wall-clock, or a GTK window never draws its first buffer

**Root cause (gdb-confirmed against a GTK 3.24.24 source build; also repros on 3.24.38).** GDK arms a window's
buffer-producing paint as a **~16.7 ms GLib timeout** (`GdkFrameClockIdlePrivate.min_next_frame_time`, `period`
= 16667 = 60 Hz vsync avoidance; see `gdk_frame_clock_paint_idle` / `maybe_start_idle` in `gdkframeclockidle.c`).
The real-GTK test drives GTK by hand with non-blocking `gtk_main_iteration_do(FALSE)`; a not-yet-due timeout is not
"pending", so `gtk_events_pending()` returns false and the pump can spin its 400 rounds **without ever dispatching
that 16 ms paint**. The window then acks its `xdg_surface.configure` but never `wl_surface.attach`es a buffer, so
the compositor never maps it and auto-hosting stalls.

- **Deterministic tell:** the FIRST GTK window in a process "accidentally" works (its cold setup happens to span
  >16 ms); every SUBSEQUENT window (warm GTK, fast) fails — loop the test body and iteration 2+ fail reliably.
- **Fix:** `GtkTestHarness.PumpOnce` sleeps ~1 ms per round so GDK's frame timeout gets wall-clock (a real app's
  `gtk_main` loop does this naturally; the standalone-C repro used `g_usleep(2000)` and never reproduced).
- **Red herrings this subsumes:** a `wl_display_flush` at init and moderate CPU load only *sped up / perturbed* the
  first window's path across the same 16 ms threshold, making the first auto-host flakily fail — not a separate bug.
  So it is NOT the embedder bind, NOT the flush, NOT our frame-clock patching, NOT the MediaContext hooks (all
  bisected out), and it does NOT affect real apps.
- The reason every in-process probe seemed to "mask" it: any added wall-clock per round pushes the loop past 16 ms,
  which *is* the fix, so instrumentation accidentally cured it.

### The real-GTK integration test is timing-sensitive — wait on the post-condition, not a fixed pump count

`GtkIntegrationTests.Real_gtk_client_maps_renders_and_embeds` drives real GTK against the compositor. GTK paints
its first buffer on its own GLib frame clock, which under load can take more than a fixed number of pump rounds.
Use `GtkTestHarness.PumpUntil(condition, maxRounds)` and assert the actual post-condition (auto-window present /
gone, embed mapped) — a fixed `PumpAll(N)` is flaky here. (The `Gdk-CRITICAL …set_dbus_properties` and "Native
blob disposal from finalizer thread" lines in the output are benign noise; they appear in passing runs too — do
not chase them as a cause.)

### GTK is a process singleton

GTK can't re-init, so the real-GTK scenarios run sequentially in one test to keep the shared display/connection
deterministic. The NWayland-client tests (`WaylandTestClient`) don't have this constraint — prefer them; they
simulate the toolkit side over the same compositor.
