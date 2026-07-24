using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NWayland;
using NWayland.Protocols.Wayland;
using NWayland.Server;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// Owns the <see cref="WaylandServer"/> and the single dispatch thread that drives it. The thread runs a
/// non-blocking epoll loop and never waits on the UI thread; the UI thread talks to it only through
/// <see cref="WaylandServer.Post"/> jobs and the deadlock-free <see cref="Roundtrip"/> primitive.
/// See planning/10-architecture-threading.md.
/// </summary>
internal sealed class WaylandCompositorWorker : IWaylandEmbedderApi
{
    private readonly WaylandServer _server;
    private readonly CompositorToUiChannel _toUi;
    private readonly CompositorState _state;
    private readonly Thread _thread;
    private readonly List<RoundtripTicket> _pendingRoundtrips = new();
    private readonly Dictionary<WaylandClient, ClientContext> _clients = new();
    private volatile bool _stop;

    public WaylandCompositorWorker(CompositorToUiChannel toUi, IWaylandServerTracer tracer)
    {
        _toUi = toUi;
        _state = new CompositorState(toUi);
        _server = new WaylandServer(new WaylandServerOptions
        {
            // UI jobs target resources the client may have destroyed moments earlier — make those no-ops.
            DisposedServerProxyCallIsNoOp = true,
            Tracer = tracer,
        });
        // UI→compositor cross-thread proxy: each call runs on this worker thread via a posted CompositorJob.
        // The marshaller is thread-safe (WaylandServer.Post is), so proxy calls are valid from any thread.
        Api = new WaylandEmbedderApiProxy(this, (action, _) => Post(new CompositorJob(action)));
        _thread = new Thread(Loop)
        {
            IsBackground = true,
            Name = "Avalonia Wayland subcompositor",
        };
        _thread.Start();
    }

    /// <summary>UI-thread-safe cross-thread proxy for UI→compositor calls (see <see cref="IWaylandEmbedderApi"/>).</summary>
    public WaylandEmbedderApiProxy Api { get; }

    /// <summary>
    /// Create a socket pair and add the server end as a client. Thread-safe (enqueues onto the compositor).
    /// Returns the client fd and a handle that drops the connection on disposal.
    /// </summary>
    public (int ClientFd, IAsyncDisposable Connection) CreateConnection()
    {
        var (client, clientFd) = _server.CreateConnectedClient();
        try
        {
            _server.Post(new SetupClientJob(client));
        }
        catch
        {
            // Post only throws if the server is disposed (not reachable in our design, but don't leak):
            // disposing the client closes the adopted server fd; the as-yet-unhanded client fd we close here.
            TryDispose(client);
            CloseFd(clientFd);
            throw;
        }
        return (clientFd, new ClientConnection(this, client));
    }

    public IAsyncDisposable AddConnection(int fd, Action<int>? release)
    {
        var client = _server.AddClient(fd, release);
        return PostSetupOrDrop(client);
    }

    public IAsyncDisposable AddConnection(Socket socket)
    {
        var client = _server.AddClient(socket);
        return PostSetupOrDrop(client);
    }

    private ClientConnection PostSetupOrDrop(WaylandClient client)
    {
        try
        {
            _server.Post(new SetupClientJob(client));
        }
        catch
        {
            TryDispose(client); // client owns the fd it was created from; Dispose closes it
            throw;
        }
        return new ClientConnection(this, client);
    }

    private static void TryDispose(WaylandClient client)
    {
        try { client.Dispose(); }
        catch { /* best-effort cleanup on a failed setup path */ }
    }

    private static void CloseFd(int fd)
    {
        // Close a raw fd without a libc P/Invoke: SafeFileHandle.Dispose() calls close() on Unix.
        try { new SafeFileHandle((IntPtr)fd, ownsHandle: true).Dispose(); }
        catch { /* best-effort */ }
    }

    public void Post(object state) => _server.Post(state);

    /// <summary>
    /// UI thread: force the compositor to fully process pending client requests, then return. While waiting
    /// the UI thread keeps draining its inbound queue, so it can never wedge the compositor (pump-while-waiting).
    /// </summary>
    public void Roundtrip()
    {
        var ticket = new RoundtripTicket();
        _server.Post(new DrainSentinel(ticket));
        while (!ticket.Completed)
        {
            // Snapshot the signal version BEFORE draining/checking so any push or completion that races in
            // afterwards bumps the version and makes WaitForSignal return immediately instead of parking.
            var version = _toUi.SignalVersion;
            _toUi.Drain();
            if (ticket.Completed)
                break;
            _toUi.WaitForSignal(version);
        }
        _toUi.Drain(); // load-bearing: applies the events the compositor enqueued before completing the ticket
    }

    public void Shutdown()
    {
        _stop = true;
        _server.Post(StopSentinel.Instance);
    }

    private void Loop()
    {
        try
        {
            while (!_stop)
            {
                while (_server.NextEventPending() is { } pending) // drain network + posted jobs (non-blocking)
                    Handle(pending);

                CompleteSatisfiedRoundtrips();                     // network caught up → release UI waiters
                if (_stop)
                    break;

                Handle(_server.NextEvent());                       // park in epoll; Post()/new fd wakes us
            }
        }
        catch (ObjectDisposedException)
        {
            // server disposed underneath us during shutdown — fall through to release waiters
        }
        finally
        {
            // never leave a synchronous Roundtrip() waiter wedged on teardown
            foreach (var ticket in _pendingRoundtrips)
                ticket.Completed = true;
            _pendingRoundtrips.Clear();
            _toUi.SignalWaiters();
        }
    }

    private void Handle(WaylandServerEvent ev)
    {
        switch (ev)
        {
            case WaylandCustomEvent custom:
                HandleCustom(custom.State);
                break;
            case WaylandServerSyncEvent sync:
                // Safe to complete immediately: all preceding requests from this client are already dispatched.
                sync.Complete(_state.NextSerial());
                break;
            case WaylandServerRequestEvent request:
                try { request.Dispatch(); }
                finally { request.Dispose(); } // always dispose — FD safety
                break;
            case WaylandServerRegistryBindEvent bind:
                if (bind.Client is { } bindClient && _clients.TryGetValue(bindClient, out var bindCtx))
                    bindCtx.HandleBind(bind);
                break;
            case WaylandClientDisconnectEvent disconnect:
                if (disconnect.Client is { } goneClient && _clients.Remove(goneClient, out var goneCtx))
                    goneCtx.Dispose();
                break;
        }
    }

    private void HandleCustom(object? state)
    {
        switch (state)
        {
            case StopSentinel:
                _stop = true;
                break;
            case SetupClientJob setup:
                var context = new ClientContext(setup.Client, _state);
                _clients[setup.Client] = context;
                context.AdvertiseGlobals();
                break;
            case DrainSentinel drain:
                _pendingRoundtrips.Add(drain.Ticket);
                break;
            case CompositorJob job:
                job.Run();
                break;
        }
    }

    // ── IWaylandEmbedderApi (runs on the compositor thread, dispatched via the Api proxy → CompositorJob) ──

    public void SetHostScale(uint hostId, int scale)
        => _state.GetRenderRootByHostId(hostId)?.Surface.SendPreferredBufferScale(scale);

    public void CloseToplevel(uint hostId)
        => _state.GetToplevelByHostId(hostId)?.SendClose();

    public void DismissPopup(uint hostId)
        => _state.GetPopupByHostId(hostId)?.SendPopupDone();

    public void SetActivated(uint hostId, bool activated)
        => _state.GetToplevelByHostId(hostId)?.SetActivated(_state, activated);

    public void ConfigureToplevel(uint hostId, int width, int height)
        => _state.GetToplevelByHostId(hostId)?.SendResizeConfigure(_state, width, height);

    public void FireFrameCallbacks(uint[] surfaceIds)
    {
        foreach (var id in surfaceIds)
            _state.GetSurfaceById(id)?.FireFrameCallbacks();
    }

    public void RegisterHostWindowExport(string handle) => _state.RegisterHostWindowExport(handle);

    public void RevokeHostWindowExport(string handle) => _state.RevokeHostWindowExport(handle);

    // No echo: the UI already holds the cookie→content-host mapping; this just makes the cookie known so
    // mark_content_surface (arriving over the toolkit's own connection) can validate it.
    public void RegisterContentCookie(string cookie) => _state.RegisterContentCookie(cookie);

    public void UnregisterContentCookie(string cookie) => _state.UnregisterContentCookie(cookie);

    // Request/response: the value is echoed to the UI via the proxy's Task; the UI drains it with Roundtrip.
    public uint RegisterEmbedTokenAsync(string token) => _state.RegisterEmbedToken(token);

    public uint ResolveForeignImportAsync(string handle) => _state.ResolveForeignImportHostId(handle);

    public void DeliverPointer(PointerInputArgs job)
    {
        // Resolve through the render root so pointer events reach popups too (a popup host id is not a toplevel).
        var xdgSurface = _state.GetRenderRootByHostId(job.HostId);
        if (xdgSurface is null)
            return;
        var root = xdgSurface.Surface;
        var client = root.Client;
        var pointers = client.Pointers;
        if (pointers.Count == 0)
            return;

        var time = unchecked((uint)(Environment.TickCount64 & 0xFFFFFFFF));
        switch (job.Kind)
        {
            case PointerEventKind.Enter:
            case PointerEventKind.Motion:
            {
                client.CurrentPointerHostId = job.HostId; // the pointer is over this host (for set_cursor routing)
                // The host maps coordinates relative to the surface geometry (content-local); add the
                // window-geometry origin to get root-surface-local coords, then hit-test the surface tree so the
                // event targets the sub-surface actually under the pointer (in ITS local coords).
                var offsetX = xdgSurface.HasGeometry ? xdgSurface.GeometryX : 0;
                var offsetY = xdgSurface.HasGeometry ? xdgSurface.GeometryY : 0;
                var target = root.HitTest(job.SurfaceX + offsetX, job.SurfaceY + offsetY, out var localX, out var localY) ?? root;
                if (!ReferenceEquals(client.PointerFocus, target))
                {
                    // Crossed a surface boundary: leave the old focus, enter the new one (wl_pointer requires an
                    // enter on a surface before motion/button reach it, and a leave before re-entering elsewhere).
                    LeavePointerFocus(client, pointers);
                    client.PointerFocus = target;
                    foreach (var pointer in pointers)
                        pointer.Enter(_state.NextSerial(), target.Resource, new WlFixed(localX), new WlFixed(localY));
                }
                if (job.Kind == PointerEventKind.Motion)
                    foreach (var pointer in pointers)
                        pointer.Motion(time, new WlFixed(localX), new WlFixed(localY));
                break;
            }
            case PointerEventKind.Leave:
                client.CurrentPointerHostId = 0;
                LeavePointerFocus(client, pointers);
                break;
            case PointerEventKind.Button:
                // button/axis carry no surface — they apply to the surface that currently holds pointer focus.
                foreach (var pointer in pointers)
                    pointer.Button(_state.NextSerial(), time, job.Button,
                        job.Pressed ? WlPointer.ButtonStateEnum.Pressed : WlPointer.ButtonStateEnum.Released);
                break;
            case PointerEventKind.Axis:
                foreach (var pointer in pointers)
                    pointer.Axis(time,
                        job.Axis == 1 ? WlPointer.AxisEnum.HorizontalScroll : WlPointer.AxisEnum.VerticalScroll,
                        new WlFixed(job.AxisValue));
                break;
        }
    }

    private void LeavePointerFocus(ClientContext client, IReadOnlyList<WlPointer.Server> pointers)
    {
        if (client.PointerFocus is not { } focus)
            return;
        foreach (var pointer in pointers)
            pointer.Leave(_state.NextSerial(), focus.Resource);
        client.PointerFocus = null;
    }

    public void DeliverKeyboard(KeyboardInputArgs job)
    {
        var xdgSurface = _state.GetRenderRootByHostId(job.HostId);
        if (xdgSurface is null)
            return;

        // NOTE: xdg_toplevel.activated is NOT driven from keyboard focus — it follows the host's containing
        // Window activation (see WaylandSubcompositorControlHost / SetActivated). wl_keyboard enter/leave below
        // is purely key-delivery focus.
        var surface = xdgSurface.Surface;
        var client = surface.Client;
        var keyboards = client.Keyboards;
        if (keyboards.Count == 0)
            return;

        var rootResource = surface.Resource;
        var time = unchecked((uint)(Environment.TickCount64 & 0xFFFFFFFF));
        switch (job.Kind)
        {
            case KeyboardEventKind.Enter:
                foreach (var keyboard in keyboards)
                {
                    keyboard.Enter(_state.NextSerial(), rootResource, ReadOnlySpan<byte>.Empty);
                    keyboard.Modifiers(_state.NextSerial(), job.Modifiers, 0, 0, 0);
                }
                client.LastSentKeyboardModifiers = job.Modifiers; // focus establishes the baseline mask
                break;
            case KeyboardEventKind.Leave:
                foreach (var keyboard in keyboards)
                    keyboard.Leave(_state.NextSerial(), rootResource);
                client.LastSentKeyboardModifiers = null; // the next enter re-establishes the mask
                break;
            case KeyboardEventKind.Key:
                // Coalesce wl_keyboard.modifiers: (re)send only when the mask changed since the last send.
                // Flush this BEFORE any IME suppression so the client's modifier state stays in lockstep even
                // when the raw key is swallowed by an active IME (a modifiers event with no key is valid).
                if (client.LastSentKeyboardModifiers != job.Modifiers)
                {
                    foreach (var keyboard in keyboards)
                        keyboard.Modifiers(_state.NextSerial(), job.Modifiers, 0, 0, 0);
                    client.LastSentKeyboardModifiers = job.Modifiers;
                }
                // An active IME delivers text via zwp_text_input_v3.commit_string; suppress the raw key for a
                // text-producing key so the client doesn't insert the character twice (raw key + commit_string).
                if (job.ProducesText && AnyTextInputEnabled(client))
                    return;
                foreach (var keyboard in keyboards)
                    keyboard.Key(_state.NextSerial(), time, job.Key,
                        job.Pressed ? WlKeyboard.KeyStateEnum.Pressed : WlKeyboard.KeyStateEnum.Released);
                break;
        }
    }

    private static bool AnyTextInputEnabled(ClientContext client)
    {
        foreach (var textInput in client.TextInputs)
            if (textInput.Enabled)
                return true;
        return false;
    }

    public void DeliverTextInput(TextInputArgs job)
    {
        var xdgSurface = _state.GetRenderRootByHostId(job.HostId);
        if (xdgSurface is null)
            return;
        var surface = xdgSurface.Surface;
        var client = surface.Client;
        // Track which host the text-input is focused on, so reverse requests (set_cursor_rectangle) route back.
        if (job.Kind == TextInputEventKind.Enter)
            client.TextInputFocusHostId = job.HostId;
        else if (job.Kind == TextInputEventKind.Leave)
            client.TextInputFocusHostId = 0;

        var textInputs = client.TextInputs;
        if (textInputs.Count == 0)
            return;

        var rootResource = surface.Resource;
        foreach (var textInput in textInputs)
        {
            switch (job.Kind)
            {
                case TextInputEventKind.Enter:
                    textInput.Resource.Enter(rootResource);
                    break;
                case TextInputEventKind.Leave:
                    if (textInput.Enabled)
                    {
                        // leave doesn't itself clear preedit (the client only updates composition on `done`), so
                        // flush a clearing preedit first or the toolkit keeps showing a stale composition.
                        textInput.Resource.PreeditString("", 0, 0);
                        textInput.Resource.Done(textInput.CommitSerial);
                    }
                    textInput.Enabled = false; // per v3: ignore input until the client re-enables after the next enter
                    textInput.Resource.Leave(rootResource);
                    break;
                case TextInputEventKind.Commit:
                    if (textInput.Enabled && job.Text is not null)
                    {
                        // Commit supersedes any pending composition: clear preedit + commit atomically on one done.
                        textInput.Resource.PreeditString("", 0, 0);
                        textInput.Resource.CommitString(job.Text);
                        textInput.Resource.Done(textInput.CommitSerial); // serial = client commit count
                    }
                    break;
                case TextInputEventKind.Preedit:
                    if (textInput.Enabled)
                    {
                        // The OS IME's composition preview (empty string clears it). cursor span is byte offsets.
                        textInput.Resource.PreeditString(job.Text, job.PreeditCursorBegin, job.PreeditCursorEnd);
                        textInput.Resource.Done(textInput.CommitSerial);
                    }
                    break;
            }
        }
    }

    private void CompleteSatisfiedRoundtrips()
    {
        if (_pendingRoundtrips.Count == 0)
            return;
        foreach (var ticket in _pendingRoundtrips)
            ticket.Completed = true;
        _pendingRoundtrips.Clear();
        _toUi.SignalWaiters();
    }
}

/// <summary>Handle returned by the connection-adding APIs; disposing drops the client on the compositor thread.</summary>
internal sealed class ClientConnection : IAsyncDisposable
{
    private readonly WaylandCompositorWorker _worker;
    private WaylandClient? _client;

    public ClientConnection(WaylandCompositorWorker worker, WaylandClient client)
    {
        _worker = worker;
        _client = client;
    }

    public ValueTask DisposeAsync()
    {
        var client = Interlocked.Exchange(ref _client, null);
        if (client is not null)
            _worker.Post(new CompositorJob(() =>
            {
                try { client.Dispose(); }
                catch { /* best-effort drop */ }
            }));
        return ValueTask.CompletedTask;
    }
}
