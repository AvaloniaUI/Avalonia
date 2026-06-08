using System.Collections.Generic;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Screens;

/// <summary>
/// Immutable per-output snapshot pushed worker → UI on every
/// add / remove / property change. <see cref="Id"/> is a process-unique
/// opaque token whose identity is stable for the lifetime of the
/// underlying wayland <c>wl_output</c> on the worker side, and is reused
/// in <c>IWSurfaceEventSink.OnSurfaceOutputsChanged</c> snapshots so the
/// UI thread can correlate per-surface output lists with screens.
/// </summary>
internal sealed record WaylandOutputSnapshot(
    object Id,
    string? Name,
    string? Description,
    string? Manufacturer,
    string? Model,
    PixelPoint LogicalPosition,
    PixelSize LogicalSize,
    int IntegerScale,
    double RefreshRateHz,
    WlOutput.SubpixelEnum Subpixel,
    WlOutput.TransformEnum Transform,
    PixelSize PhysicalSizeMm);

/// <summary>
/// Full snapshot of all outputs the compositor currently has bound and
/// fully initialised (i.e. at least their first <c>done</c> batch — and
/// matching xdg_output done if applicable — has been received). Pushed
/// to the UI thread by the worker.
/// </summary>
internal sealed record WaylandOutputsSnapshot(IReadOnlyList<WaylandOutputSnapshot> Outputs);
