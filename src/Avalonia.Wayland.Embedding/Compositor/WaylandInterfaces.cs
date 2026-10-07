using NWayland.Protocols.Plasma.ServerDecoration;
using NWayland.Protocols.TextInputUnstableV3;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgForeignUnstableV1;
using NWayland.Protocols.XdgForeignUnstableV2;
using NWayland.Protocols.XdgShell;
using AvaloniaEmbedV1 = Avalonia.Wayland.Embedding.Protocol.AvaloniaEmbedV1;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// Interface-name constants pulled from the generated <c>ProxyType.Interface.Name</c> descriptors, so a
/// typo in an advertised/bound global name is a compile error (P0-review action item) rather than a silent
/// bind failure at runtime. The versions we advertise are a separate deliberate choice (see D5) and are
/// kept as literals at the advertise site, not taken from the binding's max version.
/// </summary>
internal static class WaylandInterfaces
{
    public static readonly string Compositor = WlCompositor.ProxyType.Interface.Name;
    public static readonly string Subcompositor = WlSubcompositor.ProxyType.Interface.Name;
    public static readonly string Shm = WlShm.ProxyType.Interface.Name;
    public static readonly string Output = WlOutput.ProxyType.Interface.Name;
    public static readonly string Seat = WlSeat.ProxyType.Interface.Name;
    public static readonly string DataDeviceManager = WlDataDeviceManager.ProxyType.Interface.Name;
    public static readonly string XdgWmBase = NWayland.Protocols.XdgShell.XdgWmBase.ProxyType.Interface.Name;
    public static readonly string ServerDecorationManager = OrgKdeKwinServerDecorationManager.ProxyType.Interface.Name;
    public static readonly string TextInputManager = ZwpTextInputManagerV3.ProxyType.Interface.Name;
    public static readonly string ForeignExporter = ZxdgExporterV2.ProxyType.Interface.Name;
    public static readonly string ForeignImporter = ZxdgImporterV2.ProxyType.Interface.Name;
    // GTK3 (3.24.x) only ever binds the v1 xdg-foreign globals — it has no zxdg_exporter_v2/importer_v2 support
    // at all — so we advertise v1 as well to keep scenarios 3 & 4 working with real GTK clients.
    public static readonly string ForeignExporterV1 = ZxdgExporterV1.ProxyType.Interface.Name;
    public static readonly string ForeignImporterV1 = ZxdgImporterV1.ProxyType.Interface.Name;
    public static readonly string Embedder = AvaloniaEmbedV1.AvaloniaEmbedder.ProxyType.Interface.Name;
}
