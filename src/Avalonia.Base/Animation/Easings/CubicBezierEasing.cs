using System;

namespace Avalonia.Animation.Easings;

public class CubicBezierEasing : IEasing
{
    private CubicBezier _bezier;
    //cubic-bezier(0.25, 0.1, 0.25, 1.0)
    internal CubicBezierEasing(Point controlPoint1, Point controlPoint2)
    {
        ControlPoint1 = controlPoint1;
        ControlPoint2 = controlPoint2;
        if (controlPoint1.X < 0 || controlPoint1.X > 1 || controlPoint2.X < 0 || controlPoint2.X > 1)
            throw new ArgumentException();
        _bezier = new CubicBezier(controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y);
    }

    public Point ControlPoint2 { get; set; }
    public Point ControlPoint1 { get; set; }
    
    internal static IEasing Ease { get; } = new CubicBezierEasing(new Point(0.25, 0.1), new Point(0.25, 1));

    double IEasing.Ease(double progress)
    {
        return _bezier.Solve(progress);
    }
}