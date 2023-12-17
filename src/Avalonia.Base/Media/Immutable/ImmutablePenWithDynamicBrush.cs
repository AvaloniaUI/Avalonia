using System;
using System.Collections.Generic;

namespace Avalonia.Media.Immutable;

/// <summary>
/// Describes how a stroke is drawn.
/// </summary>
/// <remarks>
/// Unlike <see cref="ImmutablePen"/>, it accepts a mutable <see cref="Brush"/>
/// </remarks>
public class ImmutablePenWithDynamicBrush : IPen, IEquatable<IPen>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pen"/> class.
    /// </summary>
    /// <param name="brush">The brush used to draw.</param>
    /// <param name="thickness">The stroke thickness.</param>
    /// <param name="dashStyle">The dash style.</param>
    /// <param name="lineCap">The line cap.</param>
    /// <param name="lineJoin">The line join.</param>
    /// <param name="miterLimit">The miter limit.</param>
    public ImmutablePenWithDynamicBrush(
        IBrush? brush,
        double thickness = 1.0,
        ImmutableDashStyle? dashStyle = null,
        PenLineCap lineCap = PenLineCap.Flat,
        PenLineJoin lineJoin = PenLineJoin.Miter,
        double miterLimit = 10.0)
    {
        Brush = brush;
        Thickness = thickness;
        LineCap = lineCap;
        LineJoin = lineJoin;
        MiterLimit = miterLimit;
        DashStyle = dashStyle;
    }

    /// <summary>
    /// Gets the brush used to draw the stroke.
    /// </summary>
    public IBrush? Brush { get; }

    /// <summary>
    /// Gets the stroke thickness.
    /// </summary>
    public double Thickness { get; }

    /// <summary>
    /// Specifies the style of dashed lines drawn with a <see cref="Pen"/> object.
    /// </summary>
    public IDashStyle? DashStyle { get; }

    /// <summary>
    /// Specifies the type of graphic shape to use on both ends of a line.
    /// </summary>
    public PenLineCap LineCap { get; }

    /// <summary>
    /// Specifies how to join consecutive line or curve segments in a <see cref="PathFigure"/>
    /// (subpaths) contained in a <see cref="PathGeometry"/> object.
    /// </summary>
    public PenLineJoin LineJoin { get; }

    /// <summary>
    /// The limit on the ratio of the miter length to half this pen's Thickness.
    /// </summary>
    public double MiterLimit { get; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as IPen);

    /// <inheritdoc/>
    public bool Equals(IPen? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        else if (other is null)
        {
            return false;
        }

        return EqualityComparer<IBrush>.Default.Equals(Brush, other.Brush) &&
               Thickness == other.Thickness &&
               EqualityComparer<IDashStyle>.Default.Equals(DashStyle, other.DashStyle) &&
               LineCap == other.LineCap &&
               LineJoin == other.LineJoin &&
               MiterLimit == other.MiterLimit;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return (Brush, Thickness, DashStyle, LineCap, LineJoin, MiterLimit).GetHashCode();
    }
}
