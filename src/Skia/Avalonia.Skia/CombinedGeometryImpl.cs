using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Avalonia.Media.GeometryGroup"/>.
    /// </summary>
    internal class CombinedGeometryImpl : GeometryImpl
    {
        public CombinedGeometryImpl(GeometryCombineMode combineMode, Geometry g1, Geometry g2)
        {
            var path1 = (g1.PlatformImpl as GeometryImpl)?.EffectivePath;
            var path2 = (g2.PlatformImpl as GeometryImpl)?.EffectivePath;

            var op = combineMode switch
            {
                GeometryCombineMode.Intersect => SKPathOp.Intersect,
                GeometryCombineMode.Xor => SKPathOp.Xor,
                GeometryCombineMode.Exclude => SKPathOp.Difference,
                _ => SKPathOp.Union
            };

            var path = path1?.Op(path2, op);

            EffectivePath = path;
            Bounds = path?.Bounds.ToAvaloniaRect() ?? default;
        }

        public override Rect Bounds { get; }
        public override SKPath? EffectivePath { get; }
    }
}
