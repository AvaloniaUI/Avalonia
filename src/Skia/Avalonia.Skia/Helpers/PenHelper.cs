using System;
using Avalonia.Media;

namespace Avalonia.Skia.Helpers;

internal static class PenHelper
{
    /// <summary>
    /// Gets a hash code for a pen, optionally including the brush.
    /// </summary>
    /// <param name="pen">The pen.</param>
    /// <param name="includeBrush">Whether to include the brush in the hash code.</param>
    /// <returns>The hash code.</returns>
    public static int GetHashCode(IPen? pen, bool includeBrush)
    {
        if (pen is null)
            return 0;

        var hash = new HashCode();
        hash.Add(pen.LineCap);
        hash.Add(pen.LineJoin);
        hash.Add(pen.MiterLimit);
        hash.Add(pen.Thickness);

        if (pen.DashStyle is { } dashStyle)
        {
            hash.Add(dashStyle.Offset);

            for (var i = 0; i < dashStyle.Dashes?.Count; i++)
                hash.Add(dashStyle.Dashes[i]);
        }

        if (includeBrush)
            hash.Add(pen.Brush);

        return hash.ToHashCode();
    }
}
