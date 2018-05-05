using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of <see cref="IGeometryImpl"/>.
    /// </summary>
    public abstract class GeometryImpl : IGeometryImpl
    {
        /// <inheritdoc />
        public abstract Rect Bounds { get; }
        public abstract SKPath EffectivePath { get; }

        /// <inheritdoc />
        public bool FillContains(Point point)
        {
            return EffectivePath.Contains((float)point.X, (float)point.Y);
        }

        /// <inheritdoc />
        public bool StrokeContains(Pen pen, Point point)
        {
            using (var paint = new SKPaint())
            {
                paint.IsStroke = true;
                paint.StrokeWidth = (float)(pen?.Thickness ?? 0);

                using (var strokePath = new SKPath())
                {
                    paint.GetFillPath(EffectivePath, strokePath);
                    
                    return strokePath.Contains((float) point.X, (float) point.Y);
                }
            }
        }

        /// <inheritdoc />
        public IGeometryImpl Intersect(IGeometryImpl geometry)
        {
            var result = EffectivePath.Op(((GeometryImpl) geometry).EffectivePath, SKPathOp.Intersect);

            return result == null ? null : new StreamGeometryImpl(result);
        }

        /// <inheritdoc />
        public Rect GetRenderBounds(Pen pen)
        {
            var strokeThickness = pen?.Thickness ?? 0;
            
            // TODO: This is not precise, but calculating precise bounds for stroke can be quite expensive.
            return Bounds.Inflate(strokeThickness);
        }
        
        /// <inheritdoc />
        public ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            return new TransformedGeometryImpl(this, transform);
        }
    }
}
