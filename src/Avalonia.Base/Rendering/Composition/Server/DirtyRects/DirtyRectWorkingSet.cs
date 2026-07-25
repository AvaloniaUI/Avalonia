using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// A side-effect-free view over a dirty-rect tracker's <em>currently accumulating</em> working set,
/// together with the mapping from the owning host's local space to that tracker's space. The update
/// walk resolves one of these per collector swap (via <see cref="IDirtyRectCollector.GetWorkingSet"/>).
/// Reads never finalize, inflate or optimize the tracker, so they stay valid mid-pass.
/// </summary>
internal readonly struct DirtyRectWorkingSet
{
    private readonly IDirtyRectTracker? _tracker;
    public readonly DirtyRectSpaceMapping Mapping;

    public DirtyRectWorkingSet(IDirtyRectTracker? tracker, DirtyRectSpaceMapping mapping)
    {
        _tracker = tracker;
        Mapping = mapping;
    }

    /// <summary>Cheap emptiness query used by the descent gate.</summary>
    public bool IsEmpty => _tracker?.IsEmpty ?? true;

    /// <summary>
    /// Fills <paramref name="buffer"/> with the working-set rects mapped into the host's local space.
    /// </summary>
    public void CollectHostSpace(List<LtrbRect> buffer)
    {
        buffer.Clear();
        if (_tracker == null || _tracker.IsEmpty || !Mapping.IsUsable)
            return;

        _tracker.CollectWorkingSet(buffer);

        if (Mapping.IsIdentity)
            return;
        for (var i = 0; i < buffer.Count; i++)
            buffer[i] = Mapping.TrackerToHost(buffer[i]);
    }
}
