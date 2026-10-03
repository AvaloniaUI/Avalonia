using System.Collections.Generic;
using NWayland.Protocols.Plasma.ServerDecoration;
using NWayland.Protocols.TextInputUnstableV3;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgForeignUnstableV1;
using NWayland.Protocols.XdgForeignUnstableV2;
using NWayland.Protocols.XdgShell;
using NWayland.Server;
using AvaloniaEmbedV1 = Avalonia.Wayland.Embedding.Protocol.AvaloniaEmbedV1;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// Per-client state: one listener instance per global (reused across binds), the client's surfaces and
/// buffers, and the registry-bind routing. Replaces the PoC's global statics — every listener is constructed
/// with the context it belongs to, so lookups are scoped to the right <see cref="WaylandClient"/>.
/// </summary>
internal sealed class ClientContext
{
    private readonly Dictionary<WlBuffer.Server, ShmBufferState> _buffers = new();
    private readonly List<WlPointer.Server> _pointers = new();
    private readonly List<WlKeyboard.Server> _keyboards = new();
    private readonly List<TextInputState> _textInputs = new();

    public ClientContext(WaylandClient client, CompositorState state)
    {
        Client = client;
        State = state;

        Compositor = new CompositorListener(this);
        Subcompositor = new SubcompositorListener(this);
        Shm = new ShmListener(this);
        Output = new OutputListener();
        Seat = new SeatListener(this);
        DataDeviceManager = new DataDeviceManagerListener();
        XdgWmBase = new XdgWmBaseListener(this);
        Decoration = new ServerDecorationManagerListener();
        TextInputManager = new TextInputManagerListener(this);
        ForeignExporter = new ForeignExporterListenerV2(this);
        ForeignImporter = new ForeignImporterListenerV2(this);
        ForeignExporterV1 = new ForeignExporterListenerV1(this);
        ForeignImporterV1 = new ForeignImporterListenerV1(this);
        Embedder = new EmbedderListener(this);
    }

    public WaylandClient Client { get; }
    public CompositorState State { get; }
    public List<SurfaceState> Surfaces { get; } = new();

    /// <summary>Negotiated wl_compositor version (⇒ wl_surface version) for this client; 1 until bound. Lets
    /// us gate version-specific events (e.g. preferred_buffer_scale, v6) per client.</summary>
    public int CompositorVersion { get; private set; } = 1;

    /// <summary>Avalonia-side ticket associating this connection with a <c>WaylandEmbedderConnection</c>
    /// (avalonia_embedder.associate); 0 until set. Carried on a toplevel's map so the UI can scope its surface's
    /// wayland object id to THIS connection when matching resized widgets. Compositor-thread only.</summary>
    public uint Ticket { get; set; }

    /// <summary>Last modifier mask sent to this client's keyboards (null = none since the last focus leave), so
    /// DeliverKeyboard can coalesce wl_keyboard.modifiers and emit it only when the mask actually changes.</summary>
    public uint? LastSentKeyboardModifiers { get; set; }

    /// <summary>The surface this client's pointer currently has wl_pointer focus on (which may be a subsurface,
    /// not the render root). DeliverPointer hit-tests the tree and sends leave/enter as the pointer crosses
    /// surface boundaries, so motion/button reach the right sub-surface in its local coords. Compositor-thread only.</summary>
    public SurfaceState? PointerFocus { get; set; }

    // wl_pointer.set_cursor state: the client's chosen cursor surface + hotspot, and the host id the pointer is
    // currently over (so the cursor image is routed to the right host control). Compositor-thread only.
    public SurfaceState? CursorSurface { get; set; }
    public int CursorHotspotX { get; set; }
    public int CursorHotspotY { get; set; }
    public uint CurrentPointerHostId { get; set; }

    /// <summary>The host the client's text-input currently has focus on (set on text-input enter, cleared on
    /// leave), so a reverse zwp_text_input_v3 request (e.g. set_cursor_rectangle) routes to the right host's IME
    /// bridge. Compositor-thread only.</summary>
    public uint TextInputFocusHostId { get; set; }

    private CompositorListener Compositor { get; }
    private SubcompositorListener Subcompositor { get; }
    private ShmListener Shm { get; }
    private OutputListener Output { get; }
    private SeatListener Seat { get; }
    private DataDeviceManagerListener DataDeviceManager { get; }
    private XdgWmBaseListener XdgWmBase { get; }
    private ServerDecorationManagerListener Decoration { get; }
    private TextInputManagerListener TextInputManager { get; }
    private ForeignExporterListenerV2 ForeignExporter { get; }
    private ForeignImporterListenerV2 ForeignImporter { get; }
    private ForeignExporterListenerV1 ForeignExporterV1 { get; }
    private ForeignImporterListenerV1 ForeignImporterV1 { get; }
    private EmbedderListener Embedder { get; }

    public void RegisterBuffer(ShmBufferState buffer) => _buffers[buffer.Resource] = buffer;
    public void UnregisterBuffer(ShmBufferState buffer) => _buffers.Remove(buffer.Resource);
    public ShmBufferState? GetBuffer(WlBuffer.Server resource) => _buffers.GetValueOrDefault(resource);

    // The client's wl_pointer / wl_keyboard resources; input delivery sends events to all of them.
    public void RegisterPointer(WlPointer.Server pointer) => _pointers.Add(pointer);
    public void UnregisterPointer(WlPointer.Server pointer) => _pointers.Remove(pointer);
    public IReadOnlyList<WlPointer.Server> Pointers => _pointers;

    public void RegisterKeyboard(WlKeyboard.Server keyboard) => _keyboards.Add(keyboard);
    public void UnregisterKeyboard(WlKeyboard.Server keyboard) => _keyboards.Remove(keyboard);
    public IReadOnlyList<WlKeyboard.Server> Keyboards => _keyboards;

    public void RegisterTextInput(TextInputState textInput) => _textInputs.Add(textInput);
    public void UnregisterTextInput(TextInputState textInput) => _textInputs.Remove(textInput);
    public IReadOnlyList<TextInputState> TextInputs => _textInputs;

    /// <summary>Advertise our global set on this client. Names come from the generated descriptors (typo-proof).</summary>
    public void AdvertiseGlobals()
    {
        Client.AddGlobal(WaylandInterfaces.Compositor, 6); // v6 ⇒ wl_surface v6 (preferred_buffer_scale, D2)
        Client.AddGlobal(WaylandInterfaces.Subcompositor, 1);
        Client.AddGlobal(WaylandInterfaces.Shm, 1);
        Client.AddGlobal(WaylandInterfaces.Output, 1);
        Client.AddGlobal(WaylandInterfaces.Seat, 1);
        Client.AddGlobal(WaylandInterfaces.DataDeviceManager, 1);
        Client.AddGlobal(WaylandInterfaces.XdgWmBase, 3); // v3 ⇒ xdg_popup.reposition + positioner reactive/parent hints
        Client.AddGlobal(WaylandInterfaces.ServerDecorationManager, 1);
        Client.AddGlobal(WaylandInterfaces.TextInputManager, 1);
        Client.AddGlobal(WaylandInterfaces.ForeignExporter, 1);
        Client.AddGlobal(WaylandInterfaces.ForeignImporter, 1);
        Client.AddGlobal(WaylandInterfaces.ForeignExporterV1, 1); // GTK3 binds v1, not v2
        Client.AddGlobal(WaylandInterfaces.ForeignImporterV1, 1);
        Client.AddGlobal(WaylandInterfaces.Embedder, 1);
    }

    /// <summary>Bind a global to its listener and send the announce events that must precede first use.</summary>
    public void HandleBind(WaylandServerRegistryBindEvent bind)
    {
        var iface = bind.Global.Interface;
        if (iface == WaylandInterfaces.Compositor)
        {
            CompositorVersion = (int)System.Math.Min(bind.RequestedVersion, (uint)bind.Global.Version);
            bind.Accept<WlCompositor.Server>(Compositor);
        }
        else if (iface == WaylandInterfaces.Subcompositor)
            bind.Accept<WlSubcompositor.Server>(Subcompositor);
        else if (iface == WaylandInterfaces.Shm)
        {
            var shm = bind.Accept<WlShm.Server>(Shm);
            shm.Format(WlShm.FormatEnum.Argb8888);
            shm.Format(WlShm.FormatEnum.Xrgb8888);
        }
        else if (iface == WaylandInterfaces.Output)
        {
            var output = bind.Accept<WlOutput.Server>(Output);
            output.Geometry(0, 0, 300, 200, (int)WlOutput.SubpixelEnum.Unknown,
                "Avalonia", "Embedded", (int)WlOutput.TransformEnum.Normal);
            output.Mode(WlOutput.ModeEnum.Current, 1920, 1080, 60000);
        }
        else if (iface == WaylandInterfaces.Seat)
        {
            var seat = bind.Accept<WlSeat.Server>(Seat);
            seat.Capabilities(WlSeat.CapabilityEnum.Pointer | WlSeat.CapabilityEnum.Keyboard);
        }
        else if (iface == WaylandInterfaces.DataDeviceManager)
            bind.Accept<WlDataDeviceManager.Server>(DataDeviceManager);
        else if (iface == WaylandInterfaces.XdgWmBase)
            bind.Accept<XdgWmBase.Server>(XdgWmBase);
        else if (iface == WaylandInterfaces.ServerDecorationManager)
        {
            var manager = bind.Accept<OrgKdeKwinServerDecorationManager.Server>(Decoration);
            manager.DefaultMode((uint)OrgKdeKwinServerDecoration.ModeEnum.Server);
        }
        else if (iface == WaylandInterfaces.TextInputManager)
            bind.Accept<ZwpTextInputManagerV3.Server>(TextInputManager);
        else if (iface == WaylandInterfaces.ForeignExporter)
            bind.Accept<ZxdgExporterV2.Server>(ForeignExporter);
        else if (iface == WaylandInterfaces.ForeignImporter)
            bind.Accept<ZxdgImporterV2.Server>(ForeignImporter);
        else if (iface == WaylandInterfaces.ForeignExporterV1)
            bind.Accept<ZxdgExporterV1.Server>(ForeignExporterV1);
        else if (iface == WaylandInterfaces.ForeignImporterV1)
            bind.Accept<ZxdgImporterV1.Server>(ForeignImporterV1);
        else if (iface == WaylandInterfaces.Embedder)
            bind.Accept<AvaloniaEmbedV1.AvaloniaEmbedder.Server>(Embedder);
    }

    public void Dispose()
    {
        foreach (var surface in Surfaces.ToArray())
            surface.Destroy(State);
        // The client won't send wl_buffer.destroy on disconnect, so unmap any buffers it left mapped.
        foreach (var buffer in _buffers.Values)
            buffer.DisposeMapping();
        _buffers.Clear();
        _pointers.Clear();
        _keyboards.Clear();
        _textInputs.Clear();
    }
}
