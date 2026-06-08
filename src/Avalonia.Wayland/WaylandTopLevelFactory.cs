using System;
using System.Linq;
using Avalonia.FreeDesktop;
using Avalonia.Platform;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;
using Avalonia.X11;

namespace Avalonia.Wayland;

class WaylandTopLevelFactory : IWindowingPlatform
{
    private readonly WaylandWorkerClient _client;

    public WaylandTopLevelFactory(WaylandWorkerClient client)
    {
        _client = client;
    }

    public IWindowImpl CreateWindow() => new WindowImpl(_client);

    public ITopLevelImpl CreateEmbeddableTopLevel() => throw new System.NotSupportedException();

    public IWindowImpl CreateEmbeddableWindow() => throw new System.NotSupportedException();

    public ITrayIconImpl? CreateTrayIcon()
    {
        // org.kde.StatusNotifierItem (works on KDE; on GNOME via the
        // AppIndicator extension). No XEmbed fallback — XEmbed is X11-only,
        // and there is no Wayland-native systray protocol. If the watcher
        // service is not on the bus we return null and the framework falls
        // back to its own no-op handling.
        var dbusTrayIcon = new DBusTrayIconImpl();
        if (!dbusTrayIcon.IsActive)
        {
            dbusTrayIcon.Dispose();
            return null;
        }
        // DBusTrayIconImpl wants an IWindowIconImpl-to-uint[] flattener
        // (X11 NET_WM_ICON wire format: [w, h, ARGB32 pixels…]) which our
        // X11IconLoader-produced X11IconData already stores in. Reuse it.
        dbusTrayIcon.IconConverterDelegate = static icon =>
            icon is X11IconData x ? x.Data.Select(p => p.ToUInt32()).ToArray() : Array.Empty<uint>();
        return dbusTrayIcon;
    }

    public void GetWindowsZOrder(ReadOnlySpan<IWindowImpl> windows, Span<long> zOrder)
    {

    }
}