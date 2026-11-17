using System;
using Avalonia.Media;
using Avalonia.Utilities;
using SkiaSharp;

namespace Avalonia.Skia.Helpers;

internal static class SKPathHelper
{
    /// <summary>
    /// Creates a new path that is a closed version of the source path.
    /// </summary>
    /// <param name="path">The source path.</param>
    /// <returns>A closed path.</returns>
    public static SKPath CreateClosedPath(SKPath path)
    {
        using var iter = path.CreateIterator(true);
        SKPathVerb verb;
        var points = new SKPoint[4];
        using var builder = new SKPathBuilder();
        while ((verb = iter.Next(points)) != SKPathVerb.Done)
        {
            if (verb == SKPathVerb.Move)
                builder.MoveTo(points[0]);
            else if (verb == SKPathVerb.Line)
                builder.LineTo(points[1]);
            else if (verb == SKPathVerb.Close)
                builder.Close();
            else if (verb == SKPathVerb.Quad)
                builder.QuadTo(points[1], points[2]);
            else if (verb == SKPathVerb.Cubic)
                builder.CubicTo(points[1], points[2], points[3]);
            else if (verb == SKPathVerb.Conic)
                builder.ConicTo(points[1], points[2], iter.ConicWeight());

        }

        return builder.Detach();
    }

    /// <summary>
    /// Creates a path that is the result of a pen being applied to the stroke of the given path.
    /// </summary>
    /// <param name="path">The path to stroke.</param>
    /// <param name="pen">The pen to use to stroke the path.</param>
    /// <returns>The resulting path, or null if the pen has 0 thickness.</returns>
    public static SKPath? CreateStrokedPath(SKPath path, IPen pen)
    {
        if (MathUtilities.IsZero(pen.Thickness))
            return null;

        var paint = SKPaintCache.Shared.Get();
        paint.IsStroke = true;
        paint.StrokeWidth = (float)pen.Thickness;
        paint.StrokeCap = pen.LineCap.ToSKStrokeCap();
        paint.StrokeJoin = pen.LineJoin.ToSKStrokeJoin();
        paint.StrokeMiter = (float)pen.MiterLimit;

        if (DrawingContextHelper.TryCreateDashEffect(pen, out var dashEffect))
            paint.PathEffect = dashEffect;

        using var resultBuilder = new SKPathBuilder();
        paint.GetFillPath(path, resultBuilder);
        paint.PathEffect?.Dispose();
        SKPaintCache.Shared.ReturnReset(paint);
        return resultBuilder.Detach();
    }
}
