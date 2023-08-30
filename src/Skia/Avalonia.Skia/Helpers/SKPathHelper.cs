using SkiaSharp;

namespace Avalonia.Skia.Helpers;

internal static class SKPathHelper
{
    public static SKPath CreateClosedPath(SKPath path)
    {
        using var iter = path.CreateIterator(true);
        SKPathVerb verb;
        var points = new SKPoint[4];
        var rv = new SKPath();
        while ((verb = iter.Next(points)) != SKPathVerb.Done)
        {
            if (verb == SKPathVerb.Move)
                rv.MoveTo(points[0]);
            else if (verb == SKPathVerb.Line)
                rv.LineTo(points[1]);
            else if (verb == SKPathVerb.Close)
                rv.Close();
            else if (verb == SKPathVerb.Quad)
                rv.QuadTo(points[1], points[2]);
            else if (verb == SKPathVerb.Cubic)
                rv.CubicTo(points[1], points[2], points[3]);
            else if (verb == SKPathVerb.Conic)
                rv.ConicTo(points[1], points[2], iter.ConicWeight());

        }

        return rv;
    }
}
