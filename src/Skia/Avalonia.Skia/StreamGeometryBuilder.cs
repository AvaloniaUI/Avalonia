using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia;

internal sealed class StreamGeometryBuilder : IStreamGeometryBuilder
{
    private readonly SKPath _strokePath;
    private SKPath? _fillPath;
    private bool _isFilled;
    private Point _startPoint;
    private bool _isFigureBroken;
    private bool _built;

    public StreamGeometryBuilder(IGeometryImpl? source)
    {
        switch (source)
        {
            case null:
                _strokePath = CreateEmptyPath();
                _fillPath = null;
                break;
            case GeometryImpl geometry:
                _strokePath = geometry.StrokePath.Clone() ?? CreateEmptyPath();
                _fillPath = ReferenceEquals(geometry.StrokePath, geometry.FillPath) ? _strokePath : geometry.FillPath.Clone();
                break;
            default:
                throw new ArgumentException("The geometry wasn't created by the Skia render interface.", nameof(source));
        }
    }

    private SKPath Stroke
        => _strokePath;

    private SKPath Fill
        => _fillPath ??= new();

    private bool Duplicate
        => _isFilled && !ReferenceEquals(_fillPath, Stroke);

    private void EnsureSeparateFillPath()
    {
        if (ReferenceEquals(Stroke, _fillPath))
            _fillPath = Stroke.Clone();
    }

    private void BreakFigure()
    {
        if (!_isFigureBroken)
        {
            _isFigureBroken = true;
            EnsureSeparateFillPath();
        }
    }

    /// <summary>
    /// Create new empty <see cref="SKPath"/>.
    /// </summary>
    /// <returns>Empty <see cref="SKPath"/></returns>
    private static SKPath CreateEmptyPath()
    {
        return new SKPath
        {
            FillType = SKPathFillType.EvenOdd
        };
    }

    /// <inheritdoc />
    public IGeometryImpl ToGeometry()
    {
        if (_built)
            throw new InvalidOperationException("The geometry has already been built.");

        _built = true;
        return new SimpleGeometryImpl(_strokePath, _fillPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public void BeginFigure(Point startPoint, bool isFilled = true)
    {
        if (!isFilled)
            EnsureSeparateFillPath();

        _isFilled = isFilled;
        _startPoint = startPoint;
        _isFigureBroken = false;
        Stroke.MoveTo((float)startPoint.X, (float)startPoint.Y);
        if (Duplicate)
            Fill.MoveTo((float)startPoint.X, (float)startPoint.Y);
    }

    /// <inheritdoc />
    public void EndFigure(bool isClosed)
    {
        if (isClosed)
        {
            if (_isFigureBroken)
            {
                Stroke.LineTo(_startPoint.ToSKPoint());
                _isFigureBroken = false;
            }
            else
                Stroke.Close();
            if (Duplicate)
                Fill.Close();
        }
    }

    /// <inheritdoc />
    public void SetFillRule(FillRule fillRule)
    {
        Fill.FillType = fillRule == FillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
    }

    /// <inheritdoc />
    public void LineTo(Point point, bool isStroked = true)
    {
        if (isStroked)
        {
            Stroke.LineTo((float)point.X, (float)point.Y);
        }
        else
        {
            BreakFigure();
            Stroke.MoveTo((float)point.X, (float)point.Y);
        }

        if (Duplicate)
            Fill.LineTo((float)point.X, (float)point.Y);
    }

    /// <inheritdoc />
    public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
    {
        var arc = isLargeArc ? SKPathArcSize.Large : SKPathArcSize.Small;
        var sweep = sweepDirection == SweepDirection.Clockwise
            ? SKPathDirection.Clockwise
            : SKPathDirection.CounterClockwise;

        if (isStroked)
        {
            Stroke.ArcTo(
                (float)size.Width,
                (float)size.Height,
                (float)rotationAngle,
                arc,
                sweep,
                (float)point.X,
                (float)point.Y);
        }
        else
        {
            BreakFigure();
            Stroke.MoveTo((float)point.X, (float)point.Y);
        }

        if (Duplicate)
        {
            Fill.ArcTo(
                (float)size.Width,
                (float)size.Height,
                (float)rotationAngle,
                arc,
                sweep,
                (float)point.X,
                (float)point.Y);
        }
    }

    /// <inheritdoc />
    public void CubicBezierTo(Point point1, Point point2, Point point3, bool isStroked = true)
    {
        if (isStroked)
        {
            Stroke.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
        }
        else
        {
            BreakFigure();
            Stroke.MoveTo((float)point3.X, (float)point3.Y);
        }

        if (Duplicate)
            Fill.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
    }

    /// <inheritdoc />
    public void QuadraticBezierTo(Point point1, Point point2, bool isStroked = true)
    {
        if (isStroked)
        {
            Stroke.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
        }
        else
        {
            BreakFigure();
            Stroke.MoveTo((float)point2.X, (float)point2.Y);
        }

        if (Duplicate)
            Fill.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
    }
}
