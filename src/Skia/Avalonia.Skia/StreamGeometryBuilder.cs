using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia;

internal sealed class StreamGeometryBuilder : IStreamGeometryBuilder
{
    private readonly SKPathBuilder _strokeBuilder;
    private SKPathBuilder? _fillBuilder;
    private bool _fillShared;
    private bool _isFilled;
    private Point _startPoint;
    private bool _isFigureBroken;
    private bool _built;
    private Point? _trailingMove;

    public StreamGeometryBuilder(IGeometryImpl? source)
    {
        switch (source)
        {
            case null:
                _strokeBuilder = new SKPathBuilder { FillType = SKPathFillType.EvenOdd };
                _fillShared = true;
                break;
            case GeometryImpl geometry:
                _strokeBuilder = geometry.StrokePath is { } strokePath
                    ? new SKPathBuilder(strokePath) { FillType = strokePath.FillType }
                    : new SKPathBuilder { FillType = SKPathFillType.EvenOdd };
                if (ReferenceEquals(geometry.StrokePath, geometry.FillPath))
                    _fillShared = true;
                else if (geometry.FillPath is { } fillPath)
                    _fillBuilder = new SKPathBuilder(fillPath) { FillType = fillPath.FillType };
                break;
            default:
                throw new ArgumentException("The geometry wasn't created by the Skia render interface.", nameof(source));
        }
    }

    private SKPathBuilder Stroke
        => _strokeBuilder;

    private SKPathBuilder Fill
        => _fillShared ? _strokeBuilder : _fillBuilder ??= new SKPathBuilder();

    private bool Duplicate
        => _isFilled && !_fillShared;

    private void EnsureSeparateFillPath()
    {
        if (_fillShared)
        {
            using var snapshot = _strokeBuilder.Snapshot();
            _fillBuilder = new SKPathBuilder(snapshot) { FillType = _strokeBuilder.FillType };
            _fillShared = false;
        }
    }

    private void BreakFigure()
    {
        if (!_isFigureBroken)
        {
            _isFigureBroken = true;
            EnsureSeparateFillPath();
        }
    }

    // SKPathBuilder drops a trailing move, but it still counts towards the geometry bounds.
    private void MoveStroke(float x, float y)
    {
        _strokeBuilder.MoveTo(x, y);
        _trailingMove = new Point(x, y);
    }

    private static SKPath Detach(SKPathBuilder builder)
    {
        var fillType = builder.FillType;
        var path = builder.Detach();
        path.FillType = fillType;
        return path;
    }

    /// <inheritdoc />
    public IGeometryImpl ToGeometry()
    {
        if (_built)
            throw new InvalidOperationException("The geometry has already been built.");

        _built = true;
        var stroke = Detach(_strokeBuilder);
        var fill = _fillShared ? stroke : _fillBuilder is null ? null : Detach(_fillBuilder);
        var bounds = stroke.TightBounds.ToAvaloniaRect();
        if (_trailingMove is { } move)
            bounds = stroke.PointCount == 0
                ? new Rect(move, default(Size))
                : new Rect(
                    new Point(Math.Min(bounds.X, move.X), Math.Min(bounds.Y, move.Y)),
                    new Point(Math.Max(bounds.Right, move.X), Math.Max(bounds.Bottom, move.Y)));
        return new SimpleGeometryImpl(stroke, fill, bounds);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _strokeBuilder.Dispose();
        _fillBuilder?.Dispose();
    }

    /// <inheritdoc />
    public void BeginFigure(Point startPoint, bool isFilled = true)
    {
        if (!isFilled)
            EnsureSeparateFillPath();

        _isFilled = isFilled;
        _startPoint = startPoint;
        _isFigureBroken = false;
        MoveStroke((float)startPoint.X, (float)startPoint.Y);
        if (Duplicate)
            Fill.MoveTo((float)startPoint.X, (float)startPoint.Y);
    }

    /// <inheritdoc />
    public void EndFigure(bool isClosed)
    {
        if (isClosed)
        {
            _trailingMove = null;
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
            _trailingMove = null;
            Stroke.LineTo((float)point.X, (float)point.Y);
        }
        else
        {
            BreakFigure();
            MoveStroke((float)point.X, (float)point.Y);
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
            _trailingMove = null;
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
            MoveStroke((float)point.X, (float)point.Y);
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
            _trailingMove = null;
            Stroke.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
        }
        else
        {
            BreakFigure();
            MoveStroke((float)point3.X, (float)point3.Y);
        }

        if (Duplicate)
            Fill.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
    }

    /// <inheritdoc />
    public void QuadraticBezierTo(Point point1, Point point2, bool isStroked = true)
    {
        if (isStroked)
        {
            _trailingMove = null;
            Stroke.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
        }
        else
        {
            BreakFigure();
            MoveStroke((float)point2.X, (float)point2.Y);
        }

        if (Duplicate)
            Fill.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
    }
}
