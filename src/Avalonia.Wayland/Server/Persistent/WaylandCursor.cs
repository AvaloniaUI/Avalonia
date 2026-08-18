using Avalonia.Input;
using Avalonia.SourceGenerator;
using Avalonia.Wayland.Server.Transient;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>The surface + hotspot to hand to <c>wl_pointer.set_cursor</c>.</summary>
[DefinitelyNotARecord]
readonly partial struct WaylandCursorImage(WlSurface Surface, int HotspotX, int HotspotY);

/// <summary>
/// Worker-side representation of a pointer cursor. The UI thread only ever holds the generated
/// <c>WaylandCursorProxy</c> wrapper (see <see cref="IWaylandCursor"/>); resolving the actual
/// <c>wl_surface</c> happens here, on the worker thread.
/// </summary>
abstract class WaylandCursor : IWaylandCursor
{
    /// <summary>
    /// Resolves the surface + hotspot for <c>wl_pointer.set_cursor</c> on the worker thread, or
    /// <c>null</c> to hide the pointer.
    /// </summary>
    public abstract WaylandCursorImage? Resolve(WaylandGlobals globals);

    /// <inheritdoc/>
    public abstract void Destroy();
}

/// <summary>
/// A standard themed cursor. Stateless apart from the requested type — it owns no wl_surface and
/// resolves the themed surface on demand from <see cref="WaylandCursorManager"/>, so it doesn't
/// need to be a persistent object.
/// </summary>
sealed class WaylandStandardCursor(StandardCursorType cursorType) : WaylandCursor
{
    public override WaylandCursorImage? Resolve(WaylandGlobals globals)
        => globals.CursorManager.GetCursor(cursorType) is { } c
            ? new WaylandCursorImage(c.Surface, c.HotspotX, c.HotspotY)
            : null;

    // Nothing to release — the themed surfaces are owned by WaylandCursorManager.
    public override void Destroy()
    {
    }
}
