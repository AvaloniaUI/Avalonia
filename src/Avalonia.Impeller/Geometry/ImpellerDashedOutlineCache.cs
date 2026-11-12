using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Skia;
using NImpeller;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>Dashed stroke outlines of rectangles and ellipses, built at the origin. Render thread only.</summary>
internal sealed class ImpellerDashedOutlineCache : IDisposable
{
    private const int MaxIdleFrames = 120;
    private const int MaxEntries = 1024;

    private readonly Dictionary<Key, Entry> _entries = new();
    private readonly List<Key> _expired = new();
    private int _frame;

    // RoundedRect isn't IEquatable, so its parts are keyed to avoid boxing.
    private record struct Key(bool IsOval, Rect Rect, Vector TopLeft, Vector TopRight, Vector BottomRight,
        Vector BottomLeft, double Thickness, PenLineCap Cap, PenLineJoin Join, double MiterLimit, double DashOffset,
        int Dashes);

    private struct Entry
    {
        public ImpellerPath? Outline;
        public int Frame;
    }

    /// <summary>The outline of <paramref name="shape"/> (placed at the origin) stroked with a dashed pen.</summary>
    public ImpellerPath? Get(RoundedRect shape, bool isOval, IPen pen)
    {
        var key = new Key(isOval, shape.Rect, shape.RadiiTopLeft, shape.RadiiTopRight, shape.RadiiBottomRight,
            shape.RadiiBottomLeft, pen.Thickness, pen.LineCap, pen.LineJoin, pen.MiterLimit,
            pen.DashStyle?.Offset ?? 0, DashesHash(pen.DashStyle?.Dashes));

        if (_entries.TryGetValue(key, out var entry))
        {
            entry.Frame = _frame;
            _entries[key] = entry;
            return entry.Outline;
        }

        if (_entries.Count >= MaxEntries)
            Clear();

        using var path = Build(shape, isOval);
        var outline = ImpellerPaths.CreateDashedOutline(path, pen);
        _entries[key] = new Entry { Outline = outline, Frame = _frame };
        return outline;
    }

    private static SKPath Build(RoundedRect shape, bool isOval)
    {
        var path = new SKPath();
        if (isOval)
            path.AddOval(shape.Rect.ToSKRect());
        else if (shape.IsRounded)
            path.AddRoundRect(shape.ToSKRoundRect());
        else
            path.AddRect(shape.Rect.ToSKRect());
        return path;
    }

    private static int DashesHash(IReadOnlyList<double>? dashes)
    {
        var hash = new HashCode();
        for (var i = 0; i < dashes?.Count; i++)
            hash.Add(dashes[i]);
        return hash.ToHashCode();
    }

    /// <summary>Called once per presented frame to drop outlines that were not drawn for a while.</summary>
    public void EndFrame()
    {
        _frame++;
        foreach (var (key, entry) in _entries)
            if (_frame - entry.Frame > MaxIdleFrames)
                _expired.Add(key);

        foreach (var key in _expired)
        {
            _entries[key].Outline?.Dispose();
            _entries.Remove(key);
        }
        _expired.Clear();
    }

    private void Clear()
    {
        foreach (var entry in _entries.Values)
            entry.Outline?.Dispose();
        _entries.Clear();
    }

    public void Dispose() => Clear();
}
