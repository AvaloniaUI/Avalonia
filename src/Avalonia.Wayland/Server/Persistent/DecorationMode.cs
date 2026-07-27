namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// Effective xdg-decoration-v1 mode for a toplevel. Mirrors
/// <c>zxdg_toplevel_decoration_v1.mode</c> but is decoupled from the
/// NWayland type so the cross-thread sink interface doesn't pull in a
/// protocol-bindings reference on the UI side.
/// </summary>
enum DecorationMode
{
    /// <summary>Client draws its own chrome.</summary>
    ClientSide ,
    /// <summary>Compositor draws the chrome.</summary>
    ServerSide
}
