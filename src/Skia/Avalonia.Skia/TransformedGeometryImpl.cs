using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    class TransformedGeometryImpl : GeometryImpl, ITransformedGeometryImpl
    {
        public TransformedGeometryImpl(GeometryImpl source, Matrix transform)
        {
            SourceGeometry = source;
            Transform = transform;
            EffectivePath = source.EffectivePath.Clone();
            EffectivePath.Transform(transform.ToSKMatrix());
        }

        public override SKPath EffectivePath { get; }

        public IGeometryImpl SourceGeometry { get; }

        public Matrix Transform { get; }

        public override Rect Bounds => SourceGeometry.Bounds.TransformToAABB(Transform);

        public override bool FillContains(Point point)
        {
            // TODO: Not supported by SkiaSharp yet, so use expanded Rect
            return GetRenderBounds(0).Contains(point);
        }

        public override Rect GetRenderBounds(Pen pen)
        {
            return GetRenderBounds(pen.Thickness);
        }

        public override IGeometryImpl Intersect(IGeometryImpl geometry)
        {
            throw new NotImplementedException();
        }

        public override bool StrokeContains(Pen pen, Point point)
        {
            // TODO: Not supported by SkiaSharp yet, so use expanded Rect
            return GetRenderBounds(0).Contains(point);
        }

        public override ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            return new TransformedGeometryImpl(this, transform);
        }

        public Rect GetRenderBounds(double strokeThickness)
        {
            // TODO: Calculate properly.
            return Bounds.Inflate(strokeThickness);
        }
    }
}
