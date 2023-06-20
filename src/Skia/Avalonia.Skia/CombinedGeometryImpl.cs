using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Avalonia.Media.GeometryGroup"/>.
    /// </summary>
    internal class CombinedGeometryImpl : GeometryImpl
    {
        public CombinedGeometryImpl(SKPath? stroke, SKPath? fill)
        {
            StrokePath = stroke;
            FillPath = fill;
            Bounds = (stroke ?? fill)?.TightBounds.ToAvaloniaRect() ?? default;
        }

        public static CombinedGeometryImpl ForceCreate(GeometryCombineMode combineMode, IGeometryImpl g1, IGeometryImpl g2)
        {
            if (g1 is GeometryImpl i1
                && g2 is GeometryImpl i2
                && TryCreate(combineMode, i1, i2) is { } result)
                return result;
            
            return new(null, null);
        }

        public static CombinedGeometryImpl? TryCreate(GeometryCombineMode combineMode, GeometryImpl g1, GeometryImpl g2)
        {
            var op = combineMode switch
            {
                GeometryCombineMode.Intersect => SKPathOp.Intersect,
                GeometryCombineMode.Xor => SKPathOp.Xor,
                GeometryCombineMode.Exclude => SKPathOp.Difference,
                _ => SKPathOp.Union
            };

            var stroke =
                g1.StrokePath != null && g2.StrokePath != null
                    ? g1.StrokePath.Op(g2.StrokePath, op)
                    : null;

            SKPath? fill = null;
            if (g1.FillPath != null && g2.FillPath != null)
            {
                // Reuse stroke if fill paths are the same
                if (ReferenceEquals(g1.FillPath, g1.StrokePath) && ReferenceEquals(g2.FillPath, g2.StrokePath))
                    fill = stroke;
                else
                    fill = g1.FillPath.Op(g2.FillPath, op);
            }

            if (stroke == null && fill == null)
                return null;
            return new CombinedGeometryImpl(stroke, fill);
        }

        public override Rect Bounds { get; }
        public override SKPath? StrokePath { get; }
        public override SKPath? FillPath { get; }
    }
}
