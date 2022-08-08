using System;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Rendering.SceneGraph;

internal static class GeometryBoundsHelper
{
    /// <summary>
    /// Calculates the bounds of a given geometry with respect to the pens <see cref="IPen.LineCap"/>
    /// </summary>
    /// <param name="originalBounds">The calculated bounds without <see cref="IPen.LineCap"/>s</param>
    /// <param name="pen">The pen with information about the <see cref="IPen.LineCap"/>s</param>
    /// <returns></returns>
    public static Rect CalculateBoundsWithLineCaps(this Rect originalBounds, IPen? pen)
    {
        if (pen is null || MathUtilities.IsZero(pen.Thickness)) return originalBounds;

        switch (pen.LineCap)
        {
            case PenLineCap.Flat:
                return originalBounds;
            case PenLineCap.Round:
                return originalBounds.Inflate(pen.Thickness / 2);
            case PenLineCap.Square:
                return originalBounds.Inflate(pen.Thickness);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
