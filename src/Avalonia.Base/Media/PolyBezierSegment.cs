using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// PolyBezierSegment
/// </summary>
public sealed class PolyBezierSegment : PathSegment
{
    /// <summary>
    /// Points DirectProperty definition
    /// </summary>
    public static readonly DirectProperty<PolyBezierSegment, Points?> PointsProperty =
        AvaloniaProperty.RegisterDirect<PolyBezierSegment, Points?>(nameof(Points),
            o => o.Points,
            (o, v) => o.Points = v);

    private Points? _points = [];

    public PolyBezierSegment()
    {

    }

    public PolyBezierSegment(IEnumerable<Point> points, bool isStroked)
    {
        if (points is null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        Points = new Points(points);
        IsStroked = isStroked;
    }

    /// <summary>
    /// Gets or sets the Point collection that defines this <see cref="PolyBezierSegment"/> object.
    /// </summary>
    /// <value>
    /// The points.
    /// </value>
    [Metadata.Content]
    public Points? Points
    {
        get => _points;
        set => SetAndRaise(PointsProperty, ref _points, value);
    }

    internal override void ApplyTo(StreamGeometryContext ctx)
    {
        var isStroken = this.IsStroked;
        if (_points is { Count: > 0 } points)
        {
            var i = 0;
            for (; i < points.Count; i += 3)
            {
                ctx.CubicBezierTo(points[i],
                    points[i + 1],
                    points[i + 2],
                    isStroken);
            }
            var delta = i - points.Count;
            if (delta != 0)
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Warning,
                    Logging.LogArea.Visual)
                    ?.Log(nameof(PolyBezierSegment),
                        $"{nameof(PolyBezierSegment)} has ivalid number of points. Last {Math.Abs(delta)} points will be ignored.");
            }
        }
    }

    public override string ToString()
    {
        var builder = StringBuilderCache.Acquire();
        if (_points is { Count: > 0 } points)
        {
            builder.Append('C').Append(' ');
            foreach (var point in _points)
            {
                builder.Append(FormattableString.Invariant($"{point}"));
                builder.Append(' ');
            }
            builder.Length = builder.Length - 1;
        }
        return StringBuilderCache.GetStringAndRelease(builder);
    }
}
