using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// The working set of a layer: the dirty rects that are collected so far, with the mapping to layer space.
/// Reading it does not finalize, inflate or merge anything in the tracker, so it can be read while dirty
/// rects are still being collected. See <see cref="IDirtyRectCollector.GetWorkingSet"/>.
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

    public bool IsEmpty => _tracker?.IsEmpty ?? true;

    /// <summary>
    /// Replaces the contents of <paramref name="buffer"/> with the dirty rects in layer space.
    /// </summary>
    public void CopyInLayerSpaceTo(List<LtrbRect> buffer)
    {
        buffer.Clear();
        if (_tracker == null || _tracker.IsEmpty || !Mapping.IsUsable)
            return;

        _tracker.CopyWorkingSetTo(buffer);

        if (Mapping.IsIdentity)
            return;
        for (var i = 0; i < buffer.Count; i++)
            buffer[i] = Mapping.LayerPixelsToLayerSpace(buffer[i]);
    }
}
