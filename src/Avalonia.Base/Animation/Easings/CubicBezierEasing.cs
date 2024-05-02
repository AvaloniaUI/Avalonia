using System;

namespace Avalonia.Animation.Easings;

[Obsolete("Use SplineEasing instead")]
public sealed class CubicBezierEasing : IEasing
{
    private CubicBezierEasing()
    {
    }

    public Point ControlPoint2 { get; set; }
    public Point ControlPoint1 { get; set; }

    double IEasing.Ease(double progress)
        => throw new NotSupportedException();
}
