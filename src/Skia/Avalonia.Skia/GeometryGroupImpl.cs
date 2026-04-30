using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Avalonia.Media.GeometryGroup"/>.
    /// </summary>
    internal class GeometryGroupImpl : GeometryImpl
    {
        public GeometryGroupImpl(FillRule fillRule, IReadOnlyList<IGeometryImpl> children)
        {
            var fillType = fillRule == FillRule.NonZero ? SKPathFillType.Winding : SKPathFillType.EvenOdd;
            var count = children.Count;
            
            SKPath stroke;
            using (var strokeBuilder = new SKPathBuilder { FillType = fillType })
            {
                bool requiresFillPass = false;
                for (var i = 0; i < count; ++i)
                {
                    if (children[i] is GeometryImpl geo)
                    {
                        if (geo.StrokePath != null)
                            strokeBuilder.AddPath(geo.StrokePath);
                        if (!ReferenceEquals(geo.StrokePath, geo.FillPath))
                            requiresFillPass = true;
                    }
                }

                stroke = strokeBuilder.Detach();
                stroke.FillType = fillType;
                StrokePath = stroke;

                if (requiresFillPass)
                {
                    using var fillBuilder = new SKPathBuilder { FillType = fillType };

                    for (var i = 0; i < count; ++i)
                    {
                        if (children[i] is GeometryImpl { FillPath: { } fillPath })
                            fillBuilder.AddPath(fillPath);
                    }

                    var fill = fillBuilder.Detach();
                    fill.FillType = fillType;
                    FillPath = fill;
                }
                else
                    FillPath = stroke;
            }

            Bounds = stroke.TightBounds.ToAvaloniaRect();
        }

        public override Rect Bounds { get; }
        public override SKPath StrokePath { get; }
        public override SKPath FillPath { get; }
    }
}
