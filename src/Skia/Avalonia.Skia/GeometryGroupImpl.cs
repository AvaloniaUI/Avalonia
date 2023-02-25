using System.Collections.Generic;
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Avalonia.Media.GeometryGroup"/>.
    /// </summary>
    internal class GeometryGroupImpl : GeometryImpl
    {
        public GeometryGroupImpl(FillRule fillRule, IReadOnlyList<Geometry> children)
        {
            var path = new SKPath
            {
                FillType = fillRule == FillRule.NonZero ? SKPathFillType.Winding : SKPathFillType.EvenOdd,
            };

            var count = children.Count;
            
            for (var i = 0; i < count; ++i)
            {
                if (children[i].PlatformImpl is GeometryImpl { EffectivePath: { } effectivePath })
                {
                    path.AddPath(effectivePath);
                }
            }

            EffectivePath = path;
            Bounds = path.Bounds.ToAvaloniaRect();
        }

        public override Rect Bounds { get; }
        public override SKPath EffectivePath { get; }
    }
}
