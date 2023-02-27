using System;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Avalonia.Media.LineGeometry"/>.
    /// </summary>
    internal class LineGeometryImpl : GeometryImpl
    {
        public override Rect Bounds { get; }
        public override SKPath StrokePath { get; }
        public override SKPath? FillPath => null;

        public LineGeometryImpl(Point p1, Point p2)
        {
            var path = new SKPath();
            path.MoveTo(p1.ToSKPoint());
            path.LineTo(p2.ToSKPoint());

            StrokePath = path;
            Bounds = new Rect(
                new Point(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y)), 
                new Point(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y)));
        }
    }
}
