using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Wayland.Screens;

/// <summary>
/// UI-thread <see cref="IScreenImpl"/> driven entirely by snapshots
/// pushed from the wayland worker via <see cref="IWaylandOutputsSink"/>.
/// Per <c>Avalonia.Wayland</c> screen design, <see cref="Screen.Scaling"/>
/// is always <c>1.0</c>: fractional / integer scaling is a per-window
/// concern (matching the macOS model).
/// </summary>
internal sealed class SnapshotScreensImpl
    : ScreensBase<object, WaylandSnapshotScreen>, IWaylandOutputsSink
{
    private WaylandOutputsSnapshot _latest = new(Array.Empty<WaylandOutputSnapshot>());

    public void OnOutputsChanged(WaylandOutputsSnapshot snapshot)
    {
        _latest = snapshot;
        OnChanged();
    }

    internal WaylandOutputSnapshot? Lookup(object id) =>
        _latest.Outputs.FirstOrDefault(o => ReferenceEquals(o.Id, id));

    protected override IReadOnlyList<object> GetAllScreenKeys() =>
        _latest.Outputs.Select(o => o.Id).ToList();

    protected override WaylandSnapshotScreen CreateScreenFromKey(object key) =>
        new(this, key);

    protected override void ScreenAdded(WaylandSnapshotScreen screen)
    {
        screen.Refresh();
        base.ScreenAdded(screen);
    }

    protected override void ScreenChanged(WaylandSnapshotScreen screen) =>
        screen.Refresh();

    protected override Screen? ScreenFromTopLevelCore(ITopLevelImpl topLevel)
    {
        if (topLevel is WindowBaseImpl wb)
        {
            // Last-entered output is the "current" one. Walk in reverse
            // so we prefer the freshest enter.
            var ids = wb.CurrentOutputIds;
            for (var i = ids.Count - 1; i >= 0; i--)
            {
                if (TryGetScreen(ids[i], out var s))
                    return s;
            }
        }
        return base.ScreenFromTopLevelCore(topLevel);
    }
}

/// <summary>
/// One screen view-model backed by the most recent snapshot. Reads its
/// data on every <see cref="Refresh"/> from the owning
/// <see cref="SnapshotScreensImpl"/>. Identity (the platform handle) is
/// the worker-side opaque <c>Output</c> instance.
/// </summary>
internal sealed class WaylandSnapshotScreen(SnapshotScreensImpl owner, object id)
    : PlatformScreen(new WaylandScreenHandle(id))
{
    internal object Id => id;

    public void Refresh()
    {
        var snap = owner.Lookup(id);
        if (snap is null)
            return;

        DisplayName = snap.Name ?? snap.Description ?? snap.Model;
        // Per design: screen-level scale is always 1; per-window scaling
        // is owned by WSurface (preferred_buffer_scale + wp_fractional_scale).
        Scaling = 1;
        Bounds = new PixelRect(snap.LogicalPosition, snap.LogicalSize);
        // Wayland gives us no panel/strut info; treat the full bounds as
        // working area (matches Mutter / KWin client expectations).
        WorkingArea = Bounds;
    }
}

/// <summary>
/// Opaque platform handle for a Wayland screen. The handle's
/// <see cref="IPlatformHandle.Handle"/> is unused; identity is carried
/// by the wrapped <see cref="Id"/> reference equality.
/// </summary>
internal sealed class WaylandScreenHandle(object id) : IPlatformHandle, IEquatable<WaylandScreenHandle>
{
    public object Id { get; } = id;
    public IntPtr Handle => IntPtr.Zero;
    public string? HandleDescriptor => "WaylandOutput";

    public bool Equals(WaylandScreenHandle? other) =>
        other is not null && ReferenceEquals(Id, other.Id);

    public override bool Equals(object? obj) => obj is WaylandScreenHandle h && Equals(h);
    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Id);
}
