using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Avalonia.Wayland.Embedding;
using NWayland.Protocols.TextInputUnstableV3;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgForeignUnstableV1;
using NWayland.Protocols.XdgForeignUnstableV2;
using NWayland.Protocols.XdgShell;
using AvaloniaEmbed = Avalonia.Wayland.Embedding.Protocol.AvaloniaEmbedV1;

namespace Avalonia.Wayland.Embedding.Tests;

/// <summary>
/// A minimal Wayland client that stands in for an embedded toolkit (GTK, Qt, …). It connects to the
/// in-process subcompositor over the fd from <see cref="WaylandEmbeddingSubcompositor.CreateConnection"/>
/// using the NWayland CLIENT bindings (libwayland-client under the hood), binds the globals it needs, and
/// drives real surfaces/buffers so tests can assert what the compositor delivers to the Avalonia UI.
///
/// All client calls run on the test (UI) thread; the compositor runs on its own background thread, so the
/// blocking <see cref="Roundtrip"/> here only parks the test thread until the compositor replies — never a
/// deadlock.
/// </summary>
internal sealed class WaylandTestClient : IDisposable
{
    private readonly IAsyncDisposable _serverConnection;
    private readonly List<TestToplevel> _toplevels = new();
    private readonly List<TestPopup> _popups = new();
    private readonly List<object> _foreignObjects = new(); // root exported/imported proxies against finalization
    private readonly List<SharedMemoryBuffer> _shm = new();
    // Root the pool/buffer proxies so a mid-test GC can't finalize them and send wl_shm_pool.destroy /
    // wl_buffer.destroy out from under the surface they back.
    private readonly List<WlShmPool> _pools = new();
    private readonly List<WlBuffer> _buffers = new();
    private WlSeat? _seat;
    private WlPointer? _pointerProxy;
    private WlKeyboard? _keyboardProxy;
    private ZwpTextInputV3? _textInputProxy;
    private bool _disposed;

    public WlDisplay Display { get; }
    public WlCompositor Compositor { get; }
    public WlSubcompositor Subcompositor { get; }
    public WlShm Shm { get; }
    public XdgWmBase WmBase { get; }
    public AvaloniaEmbed.AvaloniaEmbedder Embedder { get; }
    public ZxdgExporterV2 ForeignExporter { get; }
    public ZxdgImporterV2 ForeignImporter { get; }
    public ZxdgExporterV1 ForeignExporterV1 { get; }
    public ZxdgImporterV1 ForeignImporterV1 { get; }

    /// <summary>Records pointer / keyboard events delivered to this client (set up in <see cref="SetupInput"/>).</summary>
    public PointerRecord Pointer { get; } = new();
    public KeyboardRecord Keyboard { get; } = new();
    public TextInputRecord TextInput { get; } = new();

    /// <summary>When true, the client enables its text-input (IME) on text-input enter — set before focusing.</summary>
    public bool AutoEnableTextInput { get; set; }

    private WaylandTestClient(IAsyncDisposable serverConnection, WlDisplay display,
        WlCompositor compositor, WlSubcompositor subcompositor, WlShm shm, XdgWmBase wmBase,
        AvaloniaEmbed.AvaloniaEmbedder embedder, ZxdgExporterV2 foreignExporter, ZxdgImporterV2 foreignImporter,
        ZxdgExporterV1 foreignExporterV1, ZxdgImporterV1 foreignImporterV1)
    {
        _serverConnection = serverConnection;
        Display = display;
        Compositor = compositor;
        Subcompositor = subcompositor;
        Shm = shm;
        WmBase = wmBase;
        Embedder = embedder;
        ForeignExporter = foreignExporter;
        ForeignImporter = foreignImporter;
        ForeignExporterV1 = foreignExporterV1;
        ForeignImporterV1 = foreignImporterV1;
    }

    public static WaylandTestClient Connect(int compositorVersion = 6)
    {
        var (clientFd, connection) = WaylandEmbeddingSubcompositor.CreateConnection();

        WlDisplay? display = null;
        try
        {
            display = WlDisplay.ConnectToFd(clientFd);

            WlCompositor? compositor = null;
            WlSubcompositor? subcompositor = null;
            WlShm? shm = null;
            WlSeat? seat = null;
            XdgWmBase? wmBase = null;
            AvaloniaEmbed.AvaloniaEmbedder? embedder = null;
            ZwpTextInputManagerV3? textInputManager = null;
            ZxdgExporterV2? foreignExporter = null;
            ZxdgImporterV2? foreignImporter = null;
            ZxdgExporterV1? foreignExporterV1 = null;
            ZxdgImporterV1? foreignImporterV1 = null;

            var registry = display.GetRegistry(new WlRegistry.Listener.Relay
            {
                OnGlobal = (reg, name, iface, version) =>
                {
                    switch (iface)
                    {
                        case "wl_compositor":
                            // v6 ⇒ wl_surface v6, so the client can receive preferred_buffer_scale; a test may
                            // request a lower version to verify the compositor withholds v6 events.
                            compositor = WlCompositor.Bind(reg, name, Math.Min(version, (uint)compositorVersion), null, null);
                            break;
                        case "wl_subcompositor":
                            subcompositor = WlSubcompositor.Bind(reg, name, Math.Min(version, 1u), null, null);
                            break;
                        case "wl_shm":
                            shm = WlShm.Bind(reg, name, Math.Min(version, 1u), null, null);
                            break;
                        case "wl_seat":
                            seat = WlSeat.Bind(reg, name, Math.Min(version, 1u), null, null);
                            break;
                        case "zwp_text_input_manager_v3":
                            textInputManager = ZwpTextInputManagerV3.Bind(reg, name, Math.Min(version, 1u), null, null);
                            break;
                        case "xdg_wm_base":
                            wmBase = XdgWmBase.Bind(reg, name, Math.Min(version, 3u),
                                new XdgWmBase.Listener.Relay { OnPing = (b, serial) => b.Pong(serial) }, null);
                            break;
                        case "avalonia_embedder":
                            embedder = AvaloniaEmbed.AvaloniaEmbedder.Bind(reg, name, Math.Min(version, 1u), null, null);
                            break;
                        case "zxdg_exporter_v2":
                            foreignExporter = ZxdgExporterV2.Bind(reg, name, Math.Min(version, 1u), null, null);
                            break;
                        case "zxdg_importer_v2":
                            foreignImporter = ZxdgImporterV2.Bind(reg, name, Math.Min(version, 1u), null, null);
                            break;
                        case "zxdg_exporter_v1":
                            foreignExporterV1 = ZxdgExporterV1.Bind(reg, name, Math.Min(version, 1u), null, null);
                            break;
                        case "zxdg_importer_v1":
                            foreignImporterV1 = ZxdgImporterV1.Bind(reg, name, Math.Min(version, 1u), null, null);
                            break;
                    }
                }
            });

            // Two roundtrips: one to receive the global announcements (and bind), one to settle the binds.
            display.Roundtrip();
            display.Roundtrip();

            if (compositor is null || subcompositor is null || shm is null || seat is null || wmBase is null
                || embedder is null || textInputManager is null || foreignExporter is null || foreignImporter is null
                || foreignExporterV1 is null || foreignImporterV1 is null)
                throw new InvalidOperationException(
                    "subcompositor did not advertise the required globals " +
                    $"(compositor={compositor is not null}, subcompositor={subcompositor is not null}, " +
                    $"shm={shm is not null}, seat={seat is not null}, xdg_wm_base={wmBase is not null}, " +
                    $"embedder={embedder is not null}, text_input={textInputManager is not null}, " +
                    $"foreign_exporter={foreignExporter is not null}, foreign_importer={foreignImporter is not null}, " +
                    $"foreign_exporter_v1={foreignExporterV1 is not null}, foreign_importer_v1={foreignImporterV1 is not null})");

            GC.KeepAlive(registry);
            var client = new WaylandTestClient(connection, display, compositor, subcompositor, shm, wmBase, embedder,
                foreignExporter, foreignImporter, foreignExporterV1, foreignImporterV1);
            client.SetupInput(seat, textInputManager);
            return client;
        }
        catch
        {
            display?.Dispose();
            connection.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw;
        }
    }

    /// <summary>Bind wl_pointer + wl_keyboard + zwp_text_input_v3 and record their events.</summary>
    private void SetupInput(WlSeat seat, ZwpTextInputManagerV3 textInputManager)
    {
        _seat = seat;
        _pointerProxy = seat.GetPointer(new WlPointer.Listener.Relay
        {
            OnEnter = (_, serial, surface, sx, sy) => Pointer.RecordEnter(serial, surface, (double)sx, (double)sy),
            OnLeave = (_, _, surface) => Pointer.RecordLeave(surface),
            OnMotion = (_, _, sx, sy) => Pointer.RecordMotion((double)sx, (double)sy),
            OnButton = (_, _, _, button, state) => Pointer.RecordButton(button, state == WlPointer.ButtonStateEnum.Pressed),
            OnAxis = (_, _, axis, value) => Pointer.RecordAxis((int)axis, (double)value),
        }, null);
        _keyboardProxy = seat.GetKeyboard(new WlKeyboard.Listener.Relay
        {
            OnKeymap = (_, _, _, _) => Keyboard.RecordKeymap(), // NWayland auto-closes the unconsumed fd after dispatch
            OnEnter = (_, _, _, _) => Keyboard.RecordEnter(),
            OnLeave = (_, _, _) => Keyboard.RecordLeave(),
            OnKey = (_, _, _, key, state) => Keyboard.RecordKey(key, state == WlKeyboard.KeyStateEnum.Pressed),
            OnModifiers = (_, _, depressed, _, _, _) => Keyboard.RecordModifiers(depressed),
        }, null);
        _textInputProxy = textInputManager.GetTextInput(seat, new ZwpTextInputV3.Listener.Relay
        {
            // On focus the compositor sends enter; a real toolkit enables IME here. Enable + commit (when the
            // test opted in) so the compositor will forward commit_string and suppress raw text keys.
            OnEnter = (ti, _) =>
            {
                TextInput.RecordEnter();
                if (AutoEnableTextInput)
                {
                    ti.Enable();
                    ti.Commit();
                }
            },
            OnLeave = (_, _) => TextInput.RecordLeave(),
            OnCommitString = (_, text) => TextInput.RecordCommitString(text),
            OnPreeditString = (_, text, cursorBegin, cursorEnd) => TextInput.RecordPreedit(text, cursorBegin, cursorEnd),
            OnDone = (_, _) => TextInput.RecordDone(),
        }, null);
        Display.Flush();
        Display.Roundtrip(); // compositor creates + registers the input objects before any input is forwarded
    }

    /// <summary>
    /// Full map dance for a toplevel: create the surface/xdg_surface/xdg_toplevel, set its identity, do the
    /// role-setup commit, wait for + ack the first configure, then attach a solid-color buffer (optionally
    /// requesting a frame callback) and commit. Returns once the client has flushed; call
    /// <see cref="Roundtrip"/>/the UI pump to make the compositor and UI catch up.
    /// </summary>
    public TestToplevel MapToplevel(string title, int width, int height, uint fillPixel,
        WlShm.FormatEnum format = WlShm.FormatEnum.Argb8888, bool requestFrameCallback = true,
        bool destroyPoolEarly = false, int minWidth = 0, int minHeight = 0, int maxWidth = 0, int maxHeight = 0,
        int bufferScale = 1)
    {
        var top = new TestToplevel(this, title);
        _toplevels.Add(top);

        var surface = Compositor.CreateSurface(new WlSurface.Listener.Relay
        {
            OnPreferredBufferScale = (_, factor) => top.RecordPreferredScale(factor),
        }, null);
        var xdgSurface = WmBase.GetXdgSurface(surface, new XdgSurface.Listener.Relay
        {
            OnConfigure = (s, serial) =>
            {
                s.AckConfigure(serial);
                top.MarkConfigured();
            }
        }, null);
        var toplevel = xdgSurface.GetToplevel(new XdgToplevel.Listener.Relay
        {
            OnConfigure = (_, w, h, states) => top.RecordConfigure(w, h, ContainsActivated(states)),
            OnClose = _ => top.MarkClosed(),
        }, null);
        toplevel.SetTitle(title);
        toplevel.SetAppId("Avalonia.Wayland.Embedding.Tests");
        if (minWidth > 0 || minHeight > 0)
            toplevel.SetMinSize(minWidth, minHeight);
        if (maxWidth > 0 || maxHeight > 0)
            toplevel.SetMaxSize(maxWidth, maxHeight);

        top.Attach(surface, xdgSurface, toplevel);

        // Role-setup commit with no buffer; the compositor answers with the initial configure.
        surface.Commit();
        Display.Flush();
        Display.Roundtrip(); // receive + ack the configure (XdgSurface relay above)

        AttachFrame(top, width, height, fillPixel, format, requestFrameCallback, destroyPoolEarly, bufferScale);
        return top;
    }

    /// <summary>
    /// Create a toplevel and complete the role-setup configure/ack handshake but attach NO buffer, so it does
    /// not map yet. Lets a test do something before the first buffer (e.g. xdg-foreign set_parent_of, which must
    /// precede the child's map); follow with <see cref="AttachFrame"/> to map it.
    /// </summary>
    public TestToplevel BeginToplevel(string title)
    {
        var top = new TestToplevel(this, title);
        _toplevels.Add(top);

        var surface = Compositor.CreateSurface(null, null);
        var xdgSurface = WmBase.GetXdgSurface(surface, new XdgSurface.Listener.Relay
        {
            OnConfigure = (s, serial) => { s.AckConfigure(serial); top.MarkConfigured(); }
        }, null);
        var toplevel = xdgSurface.GetToplevel(new XdgToplevel.Listener.Relay
        {
            OnConfigure = (_, w, h, states) => top.RecordConfigure(w, h, ContainsActivated(states)),
            OnClose = _ => top.MarkClosed(),
        }, null);
        toplevel.SetTitle(title);
        top.Attach(surface, xdgSurface, toplevel);

        surface.Commit();    // role-setup commit (no buffer) → initial configure
        Display.Flush();
        Display.Roundtrip(); // receive + ack the configure
        return top;
    }

    /// <summary>Attach a fresh solid-color buffer to an already-mapped toplevel and commit it.</summary>
    public void AttachFrame(TestToplevel top, int width, int height, uint fillPixel,
        WlShm.FormatEnum format = WlShm.FormatEnum.Argb8888, bool requestFrameCallback = true,
        bool destroyPoolEarly = false, int bufferScale = 1)
    {
        if (bufferScale > 1)
            top.Surface.SetBufferScale(bufferScale);

        var stride = width * 4;
        var size = stride * height;
        var shm = SharedMemoryBuffer.Create(size);
        _shm.Add(shm);
        shm.Fill(fillPixel);

        var pool = Shm.CreatePool(shm.Fd, size, null, null);
        var buffer = pool.CreateBuffer(0, width, height, stride, format,
            new WlBuffer.Listener.Relay { OnRelease = _ => top.RecordBufferRelease() }, null);
        _buffers.Add(buffer);

        if (destroyPoolEarly)
            pool.Destroy(); // a buffer must outlive its pool (review point #2) — the buffer keeps the memory
        else
            _pools.Add(pool); // keep alive for reuse / to avoid a finalizer destroying it mid-test

        top.Surface.Attach(buffer, 0, 0);
        top.Surface.Damage(0, 0, width, height);
        if (requestFrameCallback)
        {
            var callback = top.Surface.Frame(new WlCallback.Listener.Relay
            {
                OnDone = (_, _) => top.RecordFrameDone(),
            }, null);
            GC.KeepAlive(callback);
        }
        top.Surface.Commit();
        Display.Flush();
    }

    private WlBuffer CreateFilledBuffer(int width, int height, uint fillPixel, WlShm.FormatEnum format,
        WlBuffer.Listener? listener)
    {
        var stride = width * 4;
        var size = stride * height;
        var shm = SharedMemoryBuffer.Create(size);
        _shm.Add(shm);
        shm.Fill(fillPixel);

        var pool = Shm.CreatePool(shm.Fd, size, null, null);
        _pools.Add(pool);
        var buffer = pool.CreateBuffer(0, width, height, stride, format, listener, null);
        _buffers.Add(buffer);
        return buffer;
    }

    /// <summary>
    /// Create a synchronized subsurface of <paramref name="parent"/>, attach a solid-color buffer, and commit
    /// the CHILD only. Per wl_subsurface sync semantics the child's buffer must stay cached until the parent
    /// commits — committing the child alone must not make it visible.
    /// </summary>
    public TestSubsurface AddSyncSubsurface(TestToplevel parent, int x, int y, int width, int height,
        uint fillPixel, WlShm.FormatEnum format = WlShm.FormatEnum.Argb8888)
    {
        var child = Compositor.CreateSurface(null, null);
        var sub = Subcompositor.GetSubsurface(child, parent.Surface, null, null);
        sub.SetSync(); // explicit, though wl_subsurface starts synchronized
        sub.SetPosition(x, y);

        var buffer = CreateFilledBuffer(width, height, fillPixel, format, null);
        child.Attach(buffer, 0, 0);
        child.Damage(0, 0, width, height);
        child.Commit(); // sync subsurface: caches, must not become current yet
        Display.Flush();
        return new TestSubsurface(child, sub);
    }

    /// <summary>Commit the parent toplevel's surface — what actually applies a sync subsurface's cached state.</summary>
    public void CommitParent(TestToplevel parent)
    {
        parent.Surface.Commit();
        Display.Flush();
    }

    /// <summary>
    /// Create a toplevel and request a frame callback on the role-setup commit, BEFORE attaching any buffer
    /// (so the surface never maps). The compositor must still fire that callback rather than strand it.
    /// </summary>
    public TestToplevel CreateToplevelWithFrameCallbackBeforeBuffer(string title)
    {
        var top = new TestToplevel(this, title);
        _toplevels.Add(top);

        var surface = Compositor.CreateSurface(null, null);
        var xdgSurface = WmBase.GetXdgSurface(surface, new XdgSurface.Listener.Relay
        {
            OnConfigure = (s, serial) => { s.AckConfigure(serial); top.MarkConfigured(); }
        }, null);
        var toplevel = xdgSurface.GetToplevel(new XdgToplevel.Listener.Relay
        {
            OnConfigure = (_, w, h, states) => top.RecordConfigure(w, h, ContainsActivated(states)),
            OnClose = _ => top.MarkClosed(),
        }, null);
        toplevel.SetTitle(title);
        top.Attach(surface, xdgSurface, toplevel);

        var callback = surface.Frame(new WlCallback.Listener.Relay { OnDone = (_, _) => top.RecordFrameDone() }, null);
        GC.KeepAlive(callback);
        surface.Commit(); // no buffer attached → never maps
        Display.Flush();
        return top;
    }

    /// <summary>Request a frame callback and commit WITHOUT attaching a new buffer (no new pixels in flight).</summary>
    public void RequestFrameCallbackOnly(TestToplevel top)
    {
        var callback = top.Surface.Frame(new WlCallback.Listener.Relay { OnDone = (_, _) => top.RecordFrameDone() }, null);
        GC.KeepAlive(callback);
        top.Surface.Commit();
        Display.Flush();
    }

    /// <summary>
    /// Scenario 1: create a toplevel and embed it into the Avalonia control that minted <paramref name="token"/>
    /// (avalonia_embed.embed_toplevel sent BEFORE the surface maps), then attach a solid-color buffer and
    /// commit. The toplevel renders into that control, not an auto-window.
    /// </summary>
    public TestToplevel EmbedToplevel(string title, int width, int height, uint fillPixel, string token,
        WlShm.FormatEnum format = WlShm.FormatEnum.Argb8888)
    {
        var top = new TestToplevel(this, title);
        _toplevels.Add(top);

        var surface = Compositor.CreateSurface(null, null);
        var xdgSurface = WmBase.GetXdgSurface(surface, new XdgSurface.Listener.Relay
        {
            OnConfigure = (s, serial) => { s.AckConfigure(serial); top.MarkConfigured(); }
        }, null);
        var toplevel = xdgSurface.GetToplevel(new XdgToplevel.Listener.Relay
        {
            OnConfigure = (_, w, h, states) => top.RecordConfigure(w, h, ContainsActivated(states)),
            OnClose = _ => top.MarkClosed(),
        }, null);
        toplevel.SetTitle(title);
        top.Attach(surface, xdgSurface, toplevel);

        // Embed BEFORE the surface maps, so the compositor binds the toplevel to the token's host id.
        var result = Embedder.EmbedToplevel(surface, token, new AvaloniaEmbed.AvaloniaEmbedResult.Listener.Relay
        {
            OnBound = _ => top.RecordEmbedBound(),
            OnRejected = _ => top.RecordEmbedRejected(),
        }, null);
        GC.KeepAlive(result);

        surface.Commit();    // role setup, no buffer
        Display.Flush();
        Display.Roundtrip(); // receive + ack configure, and the embed result (bound/rejected)

        AttachFrame(top, width, height, fillPixel, format); // first buffer → maps into the embedded host
        return top;
    }

    /// <summary>Send avalonia_embed.embed_toplevel for an existing toplevel and record bound/rejected on it.</summary>
    public void Embed(TestToplevel top, string token)
    {
        var result = Embedder.EmbedToplevel(top.Surface, token, new AvaloniaEmbed.AvaloniaEmbedResult.Listener.Relay
        {
            OnBound = _ => top.RecordEmbedBound(),
            OnRejected = _ => top.RecordEmbedRejected(),
        }, null);
        GC.KeepAlive(result);
        Display.Flush();
    }

    /// <summary>
    /// Scenario 5: tag this toplevel's own surface as the container for the Avalonia content host that minted
    /// <paramref name="cookie"/> (avalonia_embed.mark_content_surface), and record bound/rejected.
    /// </summary>
    public TestEmbedResult MarkContentSurface(TestToplevel top, string cookie) => MarkContentSurface(top.Surface, cookie);

    /// <summary>Mark a popup's surface as a content container — used to verify the compositor rejects popups
    /// (content attaches only to a mapped toplevel window).</summary>
    public TestEmbedResult MarkContentSurface(TestPopup popup, string cookie) => MarkContentSurface(popup.Surface, cookie);

    private TestEmbedResult MarkContentSurface(WlSurface surface, string cookie)
    {
        var record = new TestEmbedResult();
        var result = Embedder.MarkContentSurface(surface, cookie, new AvaloniaEmbed.AvaloniaEmbedResult.Listener.Relay
        {
            OnBound = _ => record.RecordBound(),
            OnRejected = _ => record.RecordRejected(),
        }, null);
        GC.KeepAlive(result);
        Display.Flush();
        return record;
    }

    /// <summary>
    /// Map a toplevel whose single buffer lives at a non-zero, deliberately page-unaligned byte offset inside
    /// its pool — exercising the compositor's per-buffer page-alignment math (mmap a page-aligned base, then
    /// index by the in-page delta).
    /// </summary>
    public TestToplevel MapToplevelAtPoolOffset(string title, int width, int height, int offset, uint fillPixel,
        WlShm.FormatEnum format = WlShm.FormatEnum.Xrgb8888)
    {
        var top = new TestToplevel(this, title);
        _toplevels.Add(top);

        var surface = Compositor.CreateSurface(null, null);
        var xdgSurface = WmBase.GetXdgSurface(surface, new XdgSurface.Listener.Relay
        {
            OnConfigure = (s, serial) => { s.AckConfigure(serial); top.MarkConfigured(); }
        }, null);
        var toplevel = xdgSurface.GetToplevel(new XdgToplevel.Listener.Relay
        {
            OnConfigure = (_, w, h, states) => top.RecordConfigure(w, h, ContainsActivated(states)),
            OnClose = _ => top.MarkClosed(),
        }, null);
        toplevel.SetTitle(title);
        top.Attach(surface, xdgSurface, toplevel);
        surface.Commit();
        Display.Flush();
        Display.Roundtrip();

        var stride = width * 4;
        var bufferSize = stride * height;
        var poolSize = offset + bufferSize;
        var shm = SharedMemoryBuffer.Create(poolSize);
        _shm.Add(shm);
        shm.FillRange(offset, bufferSize, fillPixel);

        var pool = Shm.CreatePool(shm.Fd, poolSize, null, null);
        _pools.Add(pool);
        var buffer = pool.CreateBuffer(offset, width, height, stride, format, null, null);
        _buffers.Add(buffer);

        surface.Attach(buffer, 0, 0);
        surface.Damage(0, 0, width, height);
        surface.Commit();
        Display.Flush();
        return top;
    }

    /// <summary>
    /// Ask for a buffer whose region far exceeds its pool. The compositor can't map it, so it must bail with a
    /// fatal protocol error and disconnect us. Returns the client roundtrip result (-1 once the connection
    /// has errored).
    /// </summary>
    public int CreateOversizedBufferAndRoundtrip()
    {
        var shm = SharedMemoryBuffer.Create(4096);
        _shm.Add(shm);
        var pool = Shm.CreatePool(shm.Fd, 4096, null, null);
        _pools.Add(pool);
        // 1000x1000 @ stride 4000 = 4,000,000 bytes ≫ the 4096-byte pool → out of bounds → server bails.
        var buffer = pool.CreateBuffer(0, 1000, 1000, 4000, WlShm.FormatEnum.Xrgb8888, null, null);
        _buffers.Add(buffer);
        Display.Flush();
        return Display.Roundtrip();
    }

    /// <summary>
    /// Create a wl_shm pool with no buffers and let the compositor process it, so a test can observe
    /// pool-level behaviour such as the F_SEAL_SHRINK seal applied on creation. Returns the backing memory.
    /// </summary>
    public SharedMemoryBuffer CreatePool(int size)
    {
        var shm = SharedMemoryBuffer.Create(size);
        _shm.Add(shm);
        var pool = Shm.CreatePool(shm.Fd, size, null, null);
        _pools.Add(pool);
        Display.Flush();
        Display.Roundtrip(); // let the compositor create (and seal) the pool
        return shm;
    }

    /// <summary>Set the toplevel's window geometry (the visible rect within the surface) and commit, so the
    /// compositor offsets surface-local pointer coordinates by the geometry origin.</summary>
    public void SetWindowGeometry(TestToplevel top, int x, int y, int width, int height)
    {
        top.XdgSurface.SetWindowGeometry(x, y, width, height);
        top.Surface.Commit();
        Display.Flush();
    }

    /// <summary>wl_pointer.set_cursor: supply a custom cursor image (fresh cursor surface + buffer) with a hotspot.
    /// Requires the pointer to have entered a surface first (uses the last enter serial).</summary>
    public void SetCursor(int width, int height, uint fillPixel, int hotspotX, int hotspotY,
        WlShm.FormatEnum format = WlShm.FormatEnum.Argb8888)
    {
        var cursorSurface = Compositor.CreateSurface(null, null);
        _foreignObjects.Add(cursorSurface); // root it so a finalizer can't destroy the cursor surface mid-test
        var buffer = CreateFilledBuffer(width, height, fillPixel, format, null);
        cursorSurface.Attach(buffer, 0, 0);
        cursorSurface.Damage(0, 0, width, height);
        cursorSurface.Commit(); // commit the cursor image first; set_cursor then captures it
        _pointerProxy!.SetCursor(Pointer.LastEnterSerial, cursorSurface, hotspotX, hotspotY);
        Display.Flush();
    }

    /// <summary>wl_pointer.set_cursor(null) — clear the cursor; the host reverts to its default.</summary>
    public void HideCursor()
    {
        _pointerProxy!.SetCursor(Pointer.LastEnterSerial, null, 0, 0);
        Display.Flush();
    }

    /// <summary>zwp_text_input_v3.set_cursor_rectangle (+ commit) — report the IME caret rect to the compositor.</summary>
    public void SetTextCursorRectangle(int x, int y, int width, int height)
    {
        _textInputProxy!.SetCursorRectangle(x, y, width, height);
        _textInputProxy.Commit();
        Display.Flush();
    }

    // xdg_toplevel.configure `states` is a wl_array of little-endian uint32 enum values; true if it carries
    // the `activated` state. Accepts byte[] or ReadOnlySpan<byte> from the NWayland relay.
    private static bool ContainsActivated(ReadOnlySpan<byte> states)
    {
        for (var i = 0; i + sizeof(uint) <= states.Length; i += sizeof(uint))
            if (BinaryPrimitives.ReadUInt32LittleEndian(states[i..]) == (uint)XdgToplevel.StateEnum.Activated)
                return true;
        return false;
    }

    /// <summary>
    /// Full map dance for an xdg_popup anchored to <paramref name="parent"/>: create the surface/xdg_surface,
    /// build + snapshot an xdg_positioner, get_popup (optionally grabbing), ack the popup's positioned configure,
    /// then attach a buffer and commit so it maps into its own Avalonia Popup host.
    /// </summary>
    public TestPopup MapPopup(TestToplevel parent, int width, int height, uint fillPixel,
        int anchorRectX = 0, int anchorRectY = 0, int anchorRectWidth = 0, int anchorRectHeight = 0,
        XdgPositioner.AnchorEnum anchor = XdgPositioner.AnchorEnum.BottomLeft,
        XdgPositioner.GravityEnum gravity = XdgPositioner.GravityEnum.BottomRight,
        int offsetX = 0, int offsetY = 0, bool grab = false, TestPopup? parentPopup = null,
        WlShm.FormatEnum format = WlShm.FormatEnum.Argb8888)
    {
        var popup = new TestPopup(this);
        _popups.Add(popup);

        var surface = Compositor.CreateSurface(null, null);
        var xdgSurface = WmBase.GetXdgSurface(surface, new XdgSurface.Listener.Relay
        {
            OnConfigure = (s, serial) => { s.AckConfigure(serial); popup.MarkConfigured(); }
        }, null);

        var positioner = WmBase.CreatePositioner(null, null);
        positioner.SetSize(width, height);
        positioner.SetAnchorRect(anchorRectX, anchorRectY, anchorRectWidth, anchorRectHeight);
        positioner.SetAnchor(anchor);
        positioner.SetGravity(gravity);
        if (offsetX != 0 || offsetY != 0)
            positioner.SetOffset(offsetX, offsetY);

        // Nested popups (submenus) anchor to the parent popup's xdg_surface, not the root toplevel's.
        var parentXdgSurface = parentPopup?.XdgSurface ?? parent.XdgSurface;
        var xdgPopup = xdgSurface.GetPopup(parentXdgSurface, positioner, new XdgPopup.Listener.Relay
        {
            OnConfigure = (_, x, y, w, h) => popup.RecordConfigure(x, y, w, h),
            OnPopupDone = _ => popup.RecordPopupDone(),
            OnRepositioned = (_, token) => popup.RecordRepositioned(token),
        }, null);
        if (grab && _seat is not null)
            xdgPopup.Grab(_seat, 0); // serial 0 — the in-process compositor doesn't validate the grab serial
        positioner.Destroy(); // the server snapshotted the params at get_popup; the positioner is done

        popup.Attach(surface, xdgSurface, xdgPopup);

        surface.Commit();    // role-setup commit (no buffer)
        Display.Flush();
        Display.Roundtrip(); // receive popup.configure + xdg_surface.configure and ack it

        var buffer = CreateFilledBuffer(width, height, fillPixel, format,
            new WlBuffer.Listener.Relay { OnRelease = _ => popup.RecordBufferRelease() });
        surface.Attach(buffer, 0, 0);
        surface.Damage(0, 0, width, height);
        var callback = surface.Frame(new WlCallback.Listener.Relay { OnDone = (_, _) => popup.RecordFrameDone() }, null);
        GC.KeepAlive(callback);
        surface.Commit();    // first buffer → maps the popup
        Display.Flush();
        return popup;
    }

    /// <summary>xdg_popup.reposition: build a fresh positioner and ask the compositor to re-place the popup,
    /// echoing <paramref name="token"/> back via xdg_popup.repositioned before the new configure.</summary>
    public void RepositionPopup(TestPopup popup, int width, int height, uint token,
        int anchorRectX = 0, int anchorRectY = 0, int anchorRectWidth = 0, int anchorRectHeight = 0,
        XdgPositioner.AnchorEnum anchor = XdgPositioner.AnchorEnum.BottomLeft,
        XdgPositioner.GravityEnum gravity = XdgPositioner.GravityEnum.BottomRight,
        int offsetX = 0, int offsetY = 0)
    {
        var positioner = WmBase.CreatePositioner(null, null);
        positioner.SetSize(width, height);
        positioner.SetAnchorRect(anchorRectX, anchorRectY, anchorRectWidth, anchorRectHeight);
        positioner.SetAnchor(anchor);
        positioner.SetGravity(gravity);
        if (offsetX != 0 || offsetY != 0)
            positioner.SetOffset(offsetX, offsetY);
        popup.Popup.Reposition(positioner, token);
        positioner.Destroy();
        Display.Flush();
    }

    /// <summary>xdg-foreign: export a toplevel and capture the opaque handle the compositor sends back.</summary>
    public TestExported ExportToplevel(TestToplevel top)
    {
        var exported = new TestExported();
        var obj = ForeignExporter.ExportToplevel(top.Surface,
            new ZxdgExportedV2.Listener.Relay { OnHandle = (_, handle) => exported.RecordHandle(handle) }, null);
        exported.Attach(obj);
        _foreignObjects.Add(obj);
        Display.Flush();
        return exported;
    }

    /// <summary>xdg-foreign: import a handle (possibly unknown ⇒ inert) and make a child toplevel its child.</summary>
    public TestImported ImportToplevel(string handle)
    {
        var obj = ForeignImporter.ImportToplevel(handle, null, null);
        _foreignObjects.Add(obj);
        return new TestImported(obj);
    }

    public void SetForeignParent(TestImported imported, TestToplevel child)
    {
        imported.Imported.SetParentOf(child.Surface);
        Display.Flush();
    }

    // xdg-foreign-unstable-v1 variants (the protocol version GTK3 actually binds). Same flow as the v2 helpers
    // above, exercising the compositor's v1 globals instead of v2.
    public TestExportedV1 ExportToplevelV1(TestToplevel top)
    {
        var exported = new TestExportedV1();
        var obj = ForeignExporterV1.Export(top.Surface,
            new ZxdgExportedV1.Listener.Relay { OnHandle = (_, handle) => exported.RecordHandle(handle) }, null);
        exported.Attach(obj);
        _foreignObjects.Add(obj);
        Display.Flush();
        return exported;
    }

    public TestImportedV1 ImportToplevelV1(string handle)
    {
        var obj = ForeignImporterV1.Import(handle, null, null);
        _foreignObjects.Add(obj);
        return new TestImportedV1(obj);
    }

    public void SetForeignParentV1(TestImportedV1 imported, TestToplevel child)
    {
        imported.Imported.SetParentOf(child.Surface);
        Display.Flush();
    }

    /// <summary>Send queued requests and block until the compositor has processed all of them.</summary>
    public void Roundtrip() => Display.Roundtrip();

    public void Flush() => Display.Flush();

    /// <summary>
    /// Tear down a toplevel's objects (xdg_toplevel → xdg_surface → wl_surface), as a client closing in
    /// response to xdg_toplevel.close would. The compositor then unmaps it (→ toplevel-unmapped).
    /// </summary>
    public void DestroyToplevel(TestToplevel top)
    {
        top.Toplevel.Destroy();
        top.XdgSurface.Destroy();
        top.Surface.Destroy();
        Display.Flush();
    }

    /// <summary>Dispatch already-queued client events without blocking on the socket.</summary>
    public void DispatchPending() => Display.DispatchPending();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        // Disconnecting the client display makes the compositor observe the client as gone and unmap its
        // toplevels; the server-connection dispose then drops the server-side fd.
        Display.Dispose();
        try { _serverConnection.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        catch { /* connection may already be torn down by the disconnect above */ }

        foreach (var shm in _shm)
            shm.Dispose();
        _shm.Clear();
    }
}

/// <summary>A mapped toplevel and the event counters tests assert on (all touched on the UI thread).</summary>
internal sealed class TestToplevel
{
    private readonly WaylandTestClient _client;

    public TestToplevel(WaylandTestClient client, string title)
    {
        _client = client;
        Title = title;
    }

    public string Title { get; }
    public WlSurface Surface { get; private set; } = null!;
    public XdgSurface XdgSurface { get; private set; } = null!;
    public XdgToplevel Toplevel { get; private set; } = null!;

    public bool Configured { get; private set; }
    public bool Closed { get; private set; }
    public int ConfigureCount { get; private set; }
    public int LastConfigureWidth { get; private set; }
    public int LastConfigureHeight { get; private set; }
    public bool LastConfigureActivated { get; private set; }
    public int FrameDoneCount { get; private set; }
    public int BufferReleaseCount { get; private set; }
    public int PreferredBufferScale { get; private set; }
    public bool PreferredScaleReceived { get; private set; }
    public bool EmbedBound { get; private set; }
    public bool EmbedRejected { get; private set; }

    internal void Attach(WlSurface surface, XdgSurface xdgSurface, XdgToplevel toplevel)
    {
        Surface = surface;
        XdgSurface = xdgSurface;
        Toplevel = toplevel;
    }

    internal void MarkConfigured() => Configured = true;
    internal void MarkClosed() => Closed = true;
    internal void RecordConfigure(int width, int height, bool activated)
    {
        ConfigureCount++;
        LastConfigureWidth = width;
        LastConfigureHeight = height;
        LastConfigureActivated = activated;
    }
    internal void RecordFrameDone() => FrameDoneCount++;
    internal void RecordBufferRelease() => BufferReleaseCount++;
    internal void RecordPreferredScale(int scale)
    {
        PreferredBufferScale = scale;
        PreferredScaleReceived = true;
    }
    internal void RecordEmbedBound() => EmbedBound = true;
    internal void RecordEmbedRejected() => EmbedRejected = true;
}

/// <summary>A synchronized subsurface child created for a toplevel.</summary>
internal sealed class TestSubsurface
{
    public TestSubsurface(WlSurface surface, WlSubsurface subsurface)
    {
        Surface = surface;
        Subsurface = subsurface;
    }

    public WlSurface Surface { get; }
    public WlSubsurface Subsurface { get; }
}

/// <summary>A mapped xdg_popup and the counters tests assert on (configure geometry, popup_done, repositioned).</summary>
internal sealed class TestPopup
{
    private readonly WaylandTestClient _client;
    public TestPopup(WaylandTestClient client) => _client = client;

    public WlSurface Surface { get; private set; } = null!;
    public XdgSurface XdgSurface { get; private set; } = null!;
    public XdgPopup Popup { get; private set; } = null!;

    public bool Configured { get; private set; }
    public int ConfigureCount { get; private set; }
    public int LastX { get; private set; }
    public int LastY { get; private set; }
    public int LastWidth { get; private set; }
    public int LastHeight { get; private set; }
    public int PopupDoneCount { get; private set; }
    public int RepositionedCount { get; private set; }
    public uint LastRepositionToken { get; private set; }
    public int FrameDoneCount { get; private set; }
    public int BufferReleaseCount { get; private set; }

    internal void Attach(WlSurface surface, XdgSurface xdgSurface, XdgPopup popup)
    {
        Surface = surface;
        XdgSurface = xdgSurface;
        Popup = popup;
    }

    internal void MarkConfigured() => Configured = true;
    internal void RecordConfigure(int x, int y, int width, int height)
    {
        ConfigureCount++;
        LastX = x;
        LastY = y;
        LastWidth = width;
        LastHeight = height;
    }
    internal void RecordPopupDone() => PopupDoneCount++;
    internal void RecordRepositioned(uint token) { RepositionedCount++; LastRepositionToken = token; }
    internal void RecordFrameDone() => FrameDoneCount++;
    internal void RecordBufferRelease() => BufferReleaseCount++;

    /// <summary>Tear down the popup objects (xdg_popup → xdg_surface → wl_surface), as a client dismissing a
    /// popup would (in response to popup_done, or on its own); the compositor then unmaps it.</summary>
    public void Destroy()
    {
        Popup.Destroy();
        XdgSurface.Destroy();
        Surface.Destroy();
        _client.Flush();
    }
}

/// <summary>An xdg-foreign export and the handle the compositor published for it.</summary>
internal sealed class TestExported
{
    public ZxdgExportedV2 Exported { get; private set; } = null!;
    public string? Handle { get; private set; }
    public bool HandleReceived { get; private set; }

    internal void Attach(ZxdgExportedV2 exported) => Exported = exported;
    internal void RecordHandle(string handle) { Handle = handle; HandleReceived = true; }
}

/// <summary>An xdg-foreign import (the imported parent reference used for set_parent_of).</summary>
internal sealed class TestImported
{
    public TestImported(ZxdgImportedV2 imported) => Imported = imported;
    public ZxdgImportedV2 Imported { get; }
}

/// <summary>v1 counterpart of <see cref="TestExported"/>.</summary>
internal sealed class TestExportedV1
{
    public ZxdgExportedV1 Exported { get; private set; } = null!;
    public string? Handle { get; private set; }
    public bool HandleReceived { get; private set; }

    internal void Attach(ZxdgExportedV1 exported) => Exported = exported;
    internal void RecordHandle(string handle) { Handle = handle; HandleReceived = true; }
}

/// <summary>v1 counterpart of <see cref="TestImported"/>.</summary>
internal sealed class TestImportedV1
{
    public TestImportedV1(ZxdgImportedV1 imported) => Imported = imported;
    public ZxdgImportedV1 Imported { get; }
}

/// <summary>The one-shot result of an avalonia_embed_result (mark_content_surface / embed_toplevel).</summary>
internal sealed class TestEmbedResult
{
    public bool Bound { get; private set; }
    public bool Rejected { get; private set; }
    internal void RecordBound() => Bound = true;
    internal void RecordRejected() => Rejected = true;
}

/// <summary>Records the pointer events the compositor delivered to the client's wl_pointer.</summary>
internal sealed class PointerRecord
{
    public bool Entered { get; private set; }
    public int EnterCount { get; private set; }
    public int LeaveCount { get; private set; }
    public int MotionCount { get; private set; }
    public double LastX { get; private set; }
    public double LastY { get; private set; }
    public int ButtonCount { get; private set; }
    public uint LastButton { get; private set; }
    public bool LastButtonPressed { get; private set; }
    public int AxisCount { get; private set; }
    public int LastAxis { get; private set; }
    public double LastAxisValue { get; private set; }
    /// <summary>The surface the most recent wl_pointer.enter targeted (a subsurface, with hit-testing).</summary>
    public WlSurface? LastEnterSurface { get; private set; }
    public WlSurface? LastLeaveSurface { get; private set; }
    /// <summary>Serial of the most recent wl_pointer.enter — needed to call wl_pointer.set_cursor.</summary>
    public uint LastEnterSerial { get; private set; }

    internal void RecordEnter(uint serial, WlSurface? surface, double x, double y) { Entered = true; EnterCount++; LastEnterSerial = serial; LastEnterSurface = surface; LastX = x; LastY = y; }
    internal void RecordLeave(WlSurface? surface) { Entered = false; LeaveCount++; LastLeaveSurface = surface; }
    internal void RecordMotion(double x, double y) { MotionCount++; LastX = x; LastY = y; }
    internal void RecordButton(uint button, bool pressed) { ButtonCount++; LastButton = button; LastButtonPressed = pressed; }
    internal void RecordAxis(int axis, double value) { AxisCount++; LastAxis = axis; LastAxisValue = value; }
}

/// <summary>Records the keyboard events the compositor delivered to the client's wl_keyboard.</summary>
internal sealed class KeyboardRecord
{
    public bool KeymapReceived { get; private set; }
    public int EnterCount { get; private set; }
    public int LeaveCount { get; private set; }
    public int KeyCount { get; private set; }
    public uint LastKey { get; private set; }
    public bool LastKeyPressed { get; private set; }
    public uint LastModifiers { get; private set; }
    public int ModifiersCount { get; private set; }
    public List<uint> Keys { get; } = new();

    internal void RecordKeymap() => KeymapReceived = true;
    internal void RecordEnter() => EnterCount++;
    internal void RecordLeave() => LeaveCount++;
    internal void RecordKey(uint key, bool pressed) { KeyCount++; LastKey = key; LastKeyPressed = pressed; Keys.Add(key); }
    internal void RecordModifiers(uint depressed) { ModifiersCount++; LastModifiers = depressed; }
}

/// <summary>Records the text-input (IME) events the compositor delivered to the client's zwp_text_input_v3.</summary>
internal sealed class TextInputRecord
{
    public int EnterCount { get; private set; }
    public int LeaveCount { get; private set; }
    public int CommitStringCount { get; private set; }
    public string? LastCommit { get; private set; }
    public int DoneCount { get; private set; }
    public int PreeditCount { get; private set; }
    public string? LastPreedit { get; private set; }
    public int LastPreeditCursorBegin { get; private set; }
    public int LastPreeditCursorEnd { get; private set; }

    internal void RecordEnter() => EnterCount++;
    internal void RecordLeave() => LeaveCount++;
    internal void RecordCommitString(string? text) { CommitStringCount++; LastCommit = text; }
    internal void RecordPreedit(string? text, int cursorBegin, int cursorEnd)
    {
        PreeditCount++;
        LastPreedit = text;
        LastPreeditCursorBegin = cursorBegin;
        LastPreeditCursorEnd = cursorEnd;
    }
    internal void RecordDone() => DoneCount++;
}
