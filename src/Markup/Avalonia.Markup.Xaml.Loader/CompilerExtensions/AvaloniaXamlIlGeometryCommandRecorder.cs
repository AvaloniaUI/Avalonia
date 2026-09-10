using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal enum GeometryCommandKind
{
    SetFillRule,
    BeginFigure,
    LineTo,
    QuadraticBezierTo,
    CubicBezierTo,
    ArcTo,
    EndFigure
}

/// <summary>
/// A single recorded geometry drawing call.
/// </summary>
internal readonly struct GeometryCommand
{
    private GeometryCommand(GeometryCommandKind kind, Point point1, Point point2, Point point3,
        Size size, double rotationAngle, bool value, bool secondaryValue, int enumValue)
    {
        Kind = kind;
        Point1 = point1;
        Point2 = point2;
        Point3 = point3;
        Size = size;
        RotationAngle = rotationAngle;
        _value = value;
        _secondaryValue = secondaryValue;
        _enumValue = enumValue;
    }

    public GeometryCommandKind Kind { get; }

    public Point Point1 { get; }

    public Point Point2 { get; }

    public Point Point3 { get; }

    public Size Size { get; }

    public double RotationAngle { get; }

    private readonly bool _value;
    private readonly bool _secondaryValue;
    private readonly int _enumValue;

    public bool IsFilled => _value;

    public bool IsStroked => Kind == GeometryCommandKind.ArcTo ? _secondaryValue : _value;

    public bool IsClosed => _value;

    public bool IsLargeArc => _value;

    public FillRule FillRule => (FillRule)_enumValue;

    public SweepDirection SweepDirection => (SweepDirection)_enumValue;

    public static GeometryCommand SetFillRule(FillRule fillRule)
        => new(GeometryCommandKind.SetFillRule, default, default, default, default, 0,
            false, false, (int)fillRule);

    public static GeometryCommand BeginFigure(Point startPoint, bool isFilled)
        => new(GeometryCommandKind.BeginFigure, startPoint, default, default, default, 0,
            isFilled, false, 0);

    public static GeometryCommand LineTo(Point point, bool isStroked)
        => new(GeometryCommandKind.LineTo, point, default, default, default, 0,
            isStroked, false, 0);

    public static GeometryCommand QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked)
        => new(GeometryCommandKind.QuadraticBezierTo, controlPoint, endPoint, default, default, 0,
            isStroked, false, 0);

    public static GeometryCommand CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked)
        => new(GeometryCommandKind.CubicBezierTo, controlPoint1, controlPoint2, endPoint, default, 0,
            isStroked, false, 0);

    public static GeometryCommand ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
        SweepDirection sweepDirection, bool isStroked)
        => new(GeometryCommandKind.ArcTo, point, default, default, size, rotationAngle,
            isLargeArc, isStroked, (int)sweepDirection);

    public static GeometryCommand EndFigure(bool isClosed)
        => new(GeometryCommandKind.EndFigure, default, default, default, default, 0,
            isClosed, false, 0);
}

/// <summary>
/// A compile-time <see cref="IGeometryContext"/> which records the drawing calls made by <see cref="PathMarkupParser"/>.
/// The recorded commands are then emitted as IL by <see cref="AstNodes.AvaloniaXamlIlStreamGeometryAstNode"/>.
/// </summary>
internal sealed class AvaloniaXamlIlGeometryCommandRecorder : IGeometryContext
{
    private readonly List<GeometryCommand> _commands = [];

    public IReadOnlyList<GeometryCommand> Commands => _commands;

    public void SetFillRule(FillRule fillRule)
        => _commands.Add(GeometryCommand.SetFillRule(fillRule));

    public void BeginFigure(Point startPoint, bool isFilled = true)
        => _commands.Add(GeometryCommand.BeginFigure(startPoint, isFilled));

    public void LineTo(Point point, bool isStroked = true)
        => _commands.Add(GeometryCommand.LineTo(point, isStroked));

    public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
        => _commands.Add(GeometryCommand.QuadraticBezierTo(controlPoint, endPoint, isStroked));

    public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
        => _commands.Add(GeometryCommand.CubicBezierTo(controlPoint1, controlPoint2, endPoint, isStroked));

    public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
        SweepDirection sweepDirection, bool isStroked = true)
        => _commands.Add(GeometryCommand.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked));

    public void EndFigure(bool isClosed)
        => _commands.Add(GeometryCommand.EndFigure(isClosed));

    public void Dispose()
    {
    }
}
