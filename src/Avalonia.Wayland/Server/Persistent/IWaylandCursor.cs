using Avalonia.SourceGenerator;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// UI→worker proxy interface for a pointer cursor. The UI thread only ever holds the generated
/// <c>WaylandCursorProxy</c> wrapper and never the worker-side <see cref="WaylandCursor"/>.
/// Passing the wrapper to <see cref="IWSurface.SetCursor(IWaylandCursor)"/> auto-unwraps it to the
/// real worker object on the worker thread.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(WaylandDispatchPriority),
    "Avalonia.Wayland.Server.WaylandDispatchPriority.Normal",
    GeneratedClassName = "WaylandCursorProxy")]
internal interface IWaylandCursor
{
    /// <summary>Releases any worker-side resources held by this cursor.</summary>
    void Destroy();
}
