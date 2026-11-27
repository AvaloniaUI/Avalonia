using Avalonia.SourceGenerator;
using Avalonia.Threading;
using Avalonia.Wayland.Server;
using NWayland.Protocols.Wlr.WlrLayerShellUnstableV1;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// One <c>zwlr_layer_surface_v1.configure</c> event. The worker keeps the
/// instance it completes <see cref="WLayerSurface.BasicInitCompleted"/> with;
/// the UI sink compares by reference to skip the duplicate initial delivery.
/// </summary>
internal sealed class LayerConfigure(uint serial, int width, int height)
{
    public uint Serial { get; } = serial;

    /// <summary>Configured surface width in logical pixels.</summary>
    public int Width { get; } = width;

    /// <summary>Configured surface height in logical pixels.</summary>
    public int Height { get; } = height;
}

/// <summary>
/// UI→worker proxy interface for a <c>zwlr_layer_surface_v1</c>. Inherits the
/// common <see cref="IWSurface"/> surface API so cursor, hit-testing, scale,
/// and input plumbing work exactly like xdg toplevels and popups.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(WaylandDispatchPriority),
    "Avalonia.Wayland.Server.WaylandDispatchPriority.Normal",
    GeneratedClassName = "WLayerSurfaceProxy")]
internal interface IWLayerSurface : IWSurface
{
    /// <summary>
    /// Sets the requested surface size in logical pixels. Zero is not
    /// permitted for an anchored dimension; the caller clamps to at least 1.
    /// Double-buffered by the protocol; applied on the next commit.
    /// </summary>
    void SetSize(uint width, uint height);

    /// <summary>Sets the anchor bitmask. Double-buffered; applied on the next commit.</summary>
    void SetAnchor(ZwlrLayerSurfaceV1.AnchorEnum anchor);

    /// <summary>Sets the margin around the anchored edges. Double-buffered.</summary>
    void SetMargin(int top, int right, int bottom, int left);

    /// <summary>Sets the exclusive zone. <c>-1</c> means "ignore". Double-buffered.</summary>
    void SetExclusiveZone(int zone);

    /// <summary>Sets keyboard interactivity. Double-buffered.</summary>
    void SetKeyboardInteractivity(ZwlrLayerSurfaceV1.KeyboardInteractivityEnum mode);

    /// <summary>
    /// Stores the serial of the configure event to acknowledge on the next
    /// buffer attach. Called from the UI thread after it applied the batch.
    /// </summary>
    void SetPendingAckSerial(uint serial);
}

/// <summary>
/// Worker→UI sink for layer-surface-specific events, on top of the common
/// pointer/keyboard/touch/DnD/scale events in <see cref="IWSurfaceEventSink"/>.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(DispatcherPriority),
    "default",
    GeneratedClassName = "WLayerSurfaceEventSinkProxy")]
internal interface IWLayerSurfaceEventSink : IWSurfaceEventSink
{
    void OnConfigure(LayerConfigure configure);
    void OnClosed();
}
