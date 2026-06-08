# Wayland platform


## Special considerations

Unlike other platforms where the OS provided window server is considered to be stable, with Wayland it's EXPECTED for
compositor to crash and restart during normal usage. This means that applications need to be able to handle this situation
gracefully. So Avalonia.Wayland platform maintains its own state of the wayland surfaces in a way that can be re-uploaded
to the compositor on restart. It also means that all kinds of resources are considered to be transient and are a subject
to be re-created as needed.

Another difference with our other platform implementations, is that Wayland can't have the same render timer concept:
the compositor is controlling when the application should render next frame for EACH toplevel by sending frame callbacks.
This means that we need to react fast to said callbacks to provide acceptable frame rate. So our wayland event loop
runs on a dedicated thread that also serves as our render thread. Yes, Wayland worker thread == Avalonia's render thread.



NOTE: crash recovery is not yet supported when using an externally created wl_display.

## Protocol docs

Do NOT assume things about Wayland protocols. Those could be rather non-intuitive. Always check what the protocol says
about a particular event.

LLMs should expect to find protocol docs in `<solution_dir>/../NWayland/external/{wayland|wayland-protocols|plasma-wayland-protocols|wlr-protocols}`
Humans using LLM agents are expected to clone NWayland (https://github.com/kekekeks/NWayland) there.

NWayland generally comments protocol docs as C# XML comments in generated bindings, so those can be used as the source of information too.
LLMs should NEVER attempt to disassemble NWayland.dll or attempt to extract strings from it or use similar silly practices to extract protocol binding information, use .xml files from nuget cache instead.
NWayland converts snake_case to CamelCase, but XML docs might still be mentioning things by snake_case naming.


## Architecture 

We are using NWayland as wayland bindings. We are always using a dedicated wayland queue for all of our wayland interactions so
Avalonia can potentially be embedded into another toolkit (if said toolkit actually verifies that it gets events from objects it owns).

Wayland interactions are running on a dedicated thread that also serves as our render thread.
We are sending most of our UI->Wayland commands as a part of our composition batches, so they arrive alongside with
the information required to render frame, so the wayland thread  always works with a consistent view of the UI state.
There are also OOB commands for special cases that bypass regular Compositor's commit cycle.

## "Persistence"

To handle compositor restarts, we maintain a "persistent" state of our surfaces and resources (see Server/Persistent dir).
Entities bound to a connection defined in Server/Transient dir, they should be considered to be ephemeral.

## Render timer

Since wayland asks us nicely to NOT render when we want to, but instead tells us when to, the render timer is not an actual
timer, but something that gets triggered by frame callbacks. So if we expect the render timer to do something useful 
for e. g. new surface, we need to wake it up explicitly.
For inevitable oversights there is currently a "fallback" timer that ticks at 20FPS, but it should be removed once
we are sure that all the cases are covered (this will likely require some refactoring of UI thread's animation engine).

## Threading rules

**NO CROSS-THREAD VARIABLE ACCESS. MESSAGING ONLY.**

- Wayland interactions run on a dedicated thread (the wayland worker thread).
- UI thread objects must NEVER directly access fields of wayland-thread objects (aside from initial creation / passing to constructors).
- Wayland thread objects must NEVER directly read fields of UI-thread objects. No volatile fields, no locks, no "safe" shared state.
- All cross-thread communication from UI to wayland goes through code-generated proxies that internally route calls through `WaylandWorker.PostOob()` / `PostWithCommit()` messages (dnd/clipboard is currently an exception to this rule that we'll probably refactor later)
- ~~Input (mouse/touch/keyboard) events from wayland to UI flow through `AutomaticRawEventGrouperDispatchQueue` (enqueued on wayland thread, drained by UI thread dispatcher).~~ (this was the plan, but we need an input root to issue events from non-UI tread, so for now we simply make calls with default priority and let the auto-grouper to dispatch them, will think what to do about it later).
- Server objects (in `Server/`) should not have their internal state modified from UI thread code. The UI thread may only call `Post` and pass values into server object constructors.

## Wayland protocol rules

### Globals and versioning

- The compositor announces globals via `wl_registry.global` with the **maximum** version it supports.
- The client chooses the version it wants when calling `Bind` (should be ≤ compositor's version, ≤ bindings version).
- Use `Math.Min(compositorVersion, known-supported-version)` as the bind version; skip if below minimum required.
- Once a proxy is bound at a version, all objects created through it (factory methods or arriving as events) inherit that version.
- Events for versions higher than the bound version simply do not arrive (safe degradation).

### Globals can come and go

- Globals like `wl_seat` and `wl_output` can appear and disappear dynamically via `wl_registry.global` / `global_remove`.
- There can be **multiple** instances of the same global type simultaneously (e.g., multiple seats representing different input device groups).
- Track globals by their registry `name` (uint) so they can be properly cleaned up on `global_remove`.
- Do not assume singletons — design data structures to handle multiple instances.

### wl_pointer frame semantics

- `wl_pointer` uses frame-based event delivery: events (enter, leave, motion, button, axis) accumulate, then a `frame` event signals the end of a logical group.
- **All events within a frame must be dispatched in arrival order.** A frame can contain leave from surface A followed by enter on surface B — dispatching in an arbitrary hardcoded order will route events to the wrong surface.
- Multiple `wl_pointer.axis` events within the same frame should be combined (e.g., H+V scroll into a single vector), but the combined result must be dispatched at the correct position in the event sequence (not after all other events).
- Frame state (focused sink, position, modifiers, accumulated events) belongs to the **pointer**, not the seat. A seat manages device lifecycle; each `wl_pointer` has its own independent frame grouping.
- Button codes are Linux `input-event-codes.h` constants: `BTN_LEFT=0x110`, `BTN_RIGHT=0x111`, `BTN_MIDDLE=0x112`, `BTN_SIDE=0x113`, `BTN_EXTRA=0x114`.

### NWayland specifics

- Listeners are passed at bind/creation time (e.g., `WlSeat.Bind(..., listener)`). There is no `SetListener` method. This is needed because of known race conditions in libwayland-client.
- `WlFixed` supports explicit cast to double: `(double)surfaceX`.
- Enums like `WlSeat.CapabilityEnum` support `.HasFlag()` and `==` comparison. Do not access `.value__` (those are actually enums it's just reference API file got generated by MSFT tool that turned them into classes for some reason)
- Do NOT attempt to re-declare wayland protocol enums even if generated method accepts int/uint. This is due to XML protocol specs not actually providing machine-readable information about a particular enum being expected by the request/event. The enums themselves are still generated.
- ALWAYS specify the version range of the protocol that's supported by Avalonia. NWayland supporting a particular protocol version binding-wise does NOT mean that we are ready to support it. Even if protocol says "stable" it does't mean that there aren't new requirements or invariants for the protocol clients to support.
- Proxy version is available from Version property on all proxies. If some request is only available from version X, NWayland generates Is<Name>Available property (e. g. `public bool IsSetReactiveAvailable => Version >= 3;`) that can be used instead of `Version` checks.
