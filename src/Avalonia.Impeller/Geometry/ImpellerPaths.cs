using System;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Skia.Helpers;
using NImpeller;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>Converts Skia paths to Impeller paths.</summary>
internal static class ImpellerPaths
{
    // Skia geometry paths are immutable once shared with the render thread.
    private static readonly ConditionalWeakTable<SKPath, ImpellerPath> s_paths = new();
    private static readonly ConditionalWeakTable<SKPath, DashedOutline> s_dashed = new();

    public static ImpellerPath? Get(SKPath? path)
    {
        if (path is null)
            return null;
        if (s_paths.TryGetValue(path, out var cached))
            return cached;
        var created = Convert(path);
        if (created is null)
            return null;
        return s_paths.GetValue(path, _ => created);
    }

    public static ImpellerPath? Convert(SKPath path, SKMatrix? transform = null)
    {
        using var builder = ImpellerPathBuilder.New();
        if (builder is null)
            return null;

        using var iterator = path.CreateRawIterator();
        Span<SKPoint> points = stackalloc SKPoint[4];
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            if (transform is { } m)
                m.MapPoints(points, points);
            switch (verb)
            {
                case SKPathVerb.Move:
                    builder.MoveTo(P(points[0]));
                    break;
                case SKPathVerb.Line:
                    builder.LineTo(P(points[1]));
                    break;
                case SKPathVerb.Quad:
                    builder.QuadraticCurveTo(P(points[1]), P(points[2]));
                    break;
                case SKPathVerb.Conic:
                    AddConic(builder, points, iterator.ConicWeight());
                    break;
                case SKPathVerb.Cubic:
                    builder.CubicCurveTo(P(points[1]), P(points[2]), P(points[3]));
                    break;
                case SKPathVerb.Close:
                    builder.Close();
                    break;
            }
        }

        return builder.TakePathNew(path.FillType is SKPathFillType.EvenOdd or SKPathFillType.InverseEvenOdd
            ? ImpellerFillType.kImpellerFillTypeOdd
            : ImpellerFillType.kImpellerFillTypeNonZero);
    }

    private static void AddConic(ImpellerPathBuilder builder, ReadOnlySpan<SKPoint> points, float weight)
    {
        var quads = SKPath.ConvertConicToQuads(points[0], points[1], points[2], weight, 2);
        for (var i = 1; i + 1 < quads.Length; i += 2)
            builder.QuadraticCurveTo(P(quads[i]), P(quads[i + 1]));
    }

    private static ImpellerPoint P(SKPoint p) => new() { X = p.X, Y = p.Y };

    /// <summary>The filled outline of <paramref name="path"/> stroked with a dashed <paramref name="pen"/>.</summary>
    public static ImpellerPath? GetDashedOutline(SKPath path, IPen pen)
    {
        var key = new DashKey(pen);
        var entry = s_dashed.GetValue(path, _ => new DashedOutline());
        lock (entry)
        {
            if (entry.Path is not null && entry.Key.Equals(key))
                return entry.Path;
            entry.Path = CreateDashedOutline(path, pen);
            entry.Key = key;
            return entry.Path;
        }
    }

    public static ImpellerPath? CreateDashedOutline(SKPath path, IPen pen)
    {
        using var outline = SKPathHelper.CreateStrokedPath(path, pen);
        return outline is null ? null : Convert(outline);
    }

    public static bool IsDashed(IPen? pen) => pen?.DashStyle?.Dashes is { Count: > 0 } dashes && HasLength(dashes);

    private static bool HasLength(System.Collections.Generic.IReadOnlyList<double> dashes)
    {
        foreach (var dash in dashes)
            if (dash > 0)
                return true;
        return false;
    }

    private sealed class DashedOutline
    {
        public DashKey Key;
        public ImpellerPath? Path;
    }

    private readonly record struct DashKey(double Thickness, PenLineCap Cap, PenLineJoin Join, double Miter,
        double Offset, int Dashes)
    {
        public DashKey(IPen pen) : this(pen.Thickness, pen.LineCap, pen.LineJoin, pen.MiterLimit,
            pen.DashStyle?.Offset ?? 0, Hash(pen.DashStyle?.Dashes))
        {
        }

        private static int Hash(System.Collections.Generic.IReadOnlyList<double>? dashes)
        {
            var hash = new HashCode();
            for (var i = 0; i < dashes?.Count; i++)
                hash.Add(dashes[i]);
            return hash.ToHashCode();
        }
    }
}
