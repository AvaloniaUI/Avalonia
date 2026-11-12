using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Utilities;
using NImpeller;

namespace Avalonia.Impeller;

/// <summary>Creates Impeller paints for brushes and pens. Dispose the lease once the draw is recorded.</summary>
internal static class ImpellerPaintBuilder
{
    public readonly struct PaintLease : IDisposable
    {
        public ImpellerPaint Paint { get; }
        private readonly ImpellerColorSource? _source;
        private readonly ImpellerDrawingContext? _lender;
        private readonly bool _stroke;

        public PaintLease(ImpellerPaint paint, ImpellerColorSource? source)
        {
            Paint = paint;
            _source = source;
        }

        private PaintLease(ImpellerPaint paint, ImpellerDrawingContext lender, bool stroke)
        {
            Paint = paint;
            _lender = lender;
            _stroke = stroke;
        }

        /// <summary>A lease of the context's reusable solid-color paint.</summary>
        public static PaintLease Lent(ImpellerPaint paint, ImpellerDrawingContext lender, bool stroke) =>
            new(paint, lender, stroke);

        public void Dispose()
        {
            _source?.Dispose();
            if (_lender is not null)
                _lender.ReturnSolidPaint(_stroke);
            else
                Paint.Dispose();
        }
    }

    public static PaintLease? CreateFill(IBrush? brush, Rect bounds, ImpellerDrawingContext? owner = null,
        double opacityOverride = 1.0)
    {
        if (brush is null)
            return null;

        if (brush is ISolidColorBrush && owner?.RentSolidPaint(stroke: false) is { } solid)
        {
            solid.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleFill);
            solid.SetBlendMode(ImpellerBlendMode.kImpellerBlendModeSourceOver);
            ApplyBrush(solid, brush, bounds, opacityOverride, owner);
            return PaintLease.Lent(solid, owner, stroke: false);
        }

        var paint = ImpellerPaint.New();
        if (paint is null)
            return null;

        paint.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleFill);
        var source = ApplyBrush(paint, brush, bounds, opacityOverride, owner);
        return new PaintLease(paint, source);
    }

    public static PaintLease? CreateStroke(IPen? pen, Rect bounds, ImpellerDrawingContext? owner = null)
    {
        if (pen is null || pen.Brush is null || pen.Thickness <= 0)
            return null;

        var lent = pen.Brush is ISolidColorBrush ? owner?.RentSolidPaint(stroke: true) : null;
        var paint = lent ?? ImpellerPaint.New();
        if (paint is null)
            return null;

        paint.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        paint.SetStrokeWidth((float)pen.Thickness);
        paint.SetStrokeCap(pen.LineCap.ToImpeller());
        paint.SetStrokeJoin(pen.LineJoin.ToImpeller());
        paint.SetStrokeMiter((float)pen.MiterLimit);
        if (lent is not null)
        {
            paint.SetBlendMode(ImpellerBlendMode.kImpellerBlendModeSourceOver);
            ApplyBrush(paint, pen.Brush, bounds, 1.0, owner);
            return PaintLease.Lent(paint, owner!, stroke: true);
        }
        var source = ApplyBrush(paint, pen.Brush, bounds, 1.0, owner);
        return new PaintLease(paint, source);
    }

    private static ImpellerColorSource? ApplyBrush(ImpellerPaint paint, IBrush brush, Rect bounds,
        double opacityOverride, ImpellerDrawingContext? owner)
    {
        var opacity = brush.Opacity * opacityOverride;
        var white = new ImpellerColor
        { Alpha = 1f, Red = 1f, Green = 1f, Blue = 1f, Color_space = ImpellerColorSpace.kImpellerColorSpaceSRGB };

        switch (brush)
        {
            case ISolidColorBrush solid:
                paint.SetColor(Modulate(solid.Color, opacity));
                return null;

            case IGradientBrush gradient:
            {
                var source = CreateGradient(gradient, bounds, opacity);
                if (source is not null)
                {
                    paint.SetColor(white);
                    paint.SetColorSource(source);
                    return source;
                }
                paint.SetColor(Modulate(FirstStopColor(gradient), opacity));
                return null;
            }

            case ITileBrush or ISceneBrushContent when owner is not null:
            {
                var source = owner.MaterializeTileBrush(brush, bounds);
                if (source is not null)
                {
                    paint.SetColor(white with { Alpha = (float)opacity });
                    paint.SetColorSource(source);
                    return source;
                }
                goto default;
            }

            default:
                paint.SetColor(new ImpellerColor { Color_space = ImpellerColorSpace.kImpellerColorSpaceSRGB });
                return null;
        }
    }

    private static ImpellerColorSource? CreateGradient(IGradientBrush gradient, Rect bounds, double opacity)
    {
        var stops = gradient.GradientStops;
        if (stops.Count == 0)
            return null;

        var colors = new ImpellerColor[stops.Count];
        var offsets = new float[stops.Count];
        for (var i = 0; i < stops.Count; i++)
        {
            colors[i] = Modulate(stops[i].Color, opacity);
            offsets[i] = (float)stops[i].Offset;
        }

        var tile = gradient.SpreadMethod.ToImpeller();
        var brushTransform = GradientBrushTransform(gradient, bounds);

        switch (gradient)
        {
            case ILinearGradientBrush linear:
            {
                var start = linear.StartPoint.ToPixels(bounds).ToImpellerPoint();
                var end = linear.EndPoint.ToPixels(bounds).ToImpellerPoint();
                return ImpellerColorSource.CreateLinearGradientNew(start, end, colors, offsets, tile,
                    brushTransform?.ToImpeller());
            }
            case IRadialGradientBrush radial:
                return CreateRadialGradient(radial, bounds, colors, offsets, tile, brushTransform);
            case IConicGradientBrush conic:
            {
                var center = conic.Center.ToPixels(bounds).ToImpellerPoint();
                // Avalonia's angle 0 is at the top, Impeller's on the +X axis.
                var start = (float)(conic.Angle - 90);
                return ImpellerColorSource.CreateSweepGradientNew(center, start, start + 360f, colors, offsets, tile,
                    brushTransform?.ToImpeller());
            }
            default:
                return null;
        }
    }


    private static Matrix? GradientBrushTransform(IGradientBrush gradient, Rect bounds)
    {
        if (gradient.Transform is null)
            return null;
        var offset = Matrix.CreateTranslation(gradient.TransformOrigin.ToPixels(bounds));
        return (-offset) * gradient.Transform.Value * offset;
    }

    private static ImpellerColorSource? CreateRadialGradient(IRadialGradientBrush radial, Rect bounds,
        ImpellerColor[] colors, float[] offsets, ImpellerTileMode tile, Matrix? brushTransform)
    {
        var centerPoint = radial.Center.ToPixels(bounds);
        var center = centerPoint.ToImpellerPoint();
        var radiusX = (float)radial.RadiusX.ToValue(bounds.Width);
        var radiusY = (float)radial.RadiusY.ToValue(bounds.Height);
        var originPoint = radial.GradientOrigin.ToPixels(bounds);

        Matrix? transform = null;
        if (radiusX != radiusY && radiusX != 0)
            transform = Matrix.CreateTranslation(-centerPoint)
                        * Matrix.CreateScale(1, radiusY / radiusX)
                        * Matrix.CreateTranslation(centerPoint);
        if (brushTransform is { } bt)
            transform = transform.HasValue ? transform * bt : bt;

        if (originPoint.Equals(centerPoint))
            return ImpellerColorSource.CreateRadialGradientNew(center, radiusX, colors, offsets, tile,
                transform?.ToImpeller());

        // A focal point is a two-point conical gradient, as in Skia.
        if (radiusX != radiusY && radiusY != 0)
            originPoint = originPoint.WithY((originPoint.Y - centerPoint.Y) * radiusX / radiusY + centerPoint.Y);

        var startCenter = originPoint.ToImpellerPoint();
        var startRadius = 0f;
        var endCenter = center;
        var endRadius = radiusX;

        if (offsets[^1] == 1f)
        {
            (startCenter, startRadius, endCenter, endRadius) = (endCenter, endRadius, startCenter, startRadius);
            var count = offsets.Length;
            var reversedColors = new ImpellerColor[count];
            var reversedOffsets = new float[count];
            for (var i = 0; i < count; i++)
            {
                var off = 1 - (float)radial.GradientStops[i].Offset;
                if (MathUtilities.IsZero(off))
                    off = 0;
                reversedColors[count - 1 - i] = colors[i];
                reversedOffsets[count - 1 - i] = off;
            }
            colors = reversedColors;
            offsets = reversedOffsets;
        }

        return ImpellerColorSource.CreateConicalGradientNew(startCenter, startRadius, endCenter, endRadius,
            colors, offsets, tile, transform?.ToImpeller());
    }

    private static ImpellerColor Modulate(Color c, double opacity)
    {
        var a = Math.Clamp(c.A / 255.0 * opacity, 0, 1);
        return new ImpellerColor
        {
            Alpha = (float)a,
            Red = c.R / 255f,
            Green = c.G / 255f,
            Blue = c.B / 255f,
            Color_space = ImpellerColorSpace.kImpellerColorSpaceSRGB
        };
    }

    private static Color FirstStopColor(IGradientBrush gradient)
    {
        IReadOnlyList<IGradientStop> stops = gradient.GradientStops;
        return stops.Count > 0 ? stops[0].Color : Colors.Transparent;
    }
}
