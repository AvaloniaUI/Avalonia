using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Transient;

namespace Avalonia.Wayland.Server.Persistent;

interface IPersistentWaylandObject
{
    void OnConnected(WaylandConnection connection, WaylandGlobals globals);
    void OnDisconnected();
}