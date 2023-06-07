using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Avalonia.Media.EllipseGeometry"/>.
    /// </summary>
    internal class EllipseGeometryImpl : GeometryImpl
    {
        public override Rect Bounds { get; }
        public override SKPath StrokePath { get; }
        public override SKPath FillPath => StrokePath;

        public EllipseGeometryImpl(Rect rect)
        {
            var path = new SKPath();
            path.AddOval(rect.ToSKRect());

            StrokePath = path;
            Bounds = rect;
        }
    }
}
