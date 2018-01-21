using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    abstract class GeometryImpl : IGeometryImpl
    {
        public abstract Rect Bounds { get; }
        public abstract SKPath EffectivePath { get; }
        public abstract bool FillContains(Point point);
        public abstract Rect GetRenderBounds(Pen pen);
        public abstract IGeometryImpl Intersect(IGeometryImpl geometry);
        public abstract bool StrokeContains(Pen pen, Point point);
        public abstract ITransformedGeometryImpl WithTransform(Matrix transform);
    }
}
