using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Avalonia.Media.RectangleGeometry"/>.
    /// </summary>
    internal class RectangleGeometryImpl : GeometryImpl
    {
        public override Rect Bounds { get; }
        public override SKPath StrokePath { get; }
        public override SKPath? FillPath => StrokePath;

        public RectangleGeometryImpl(Rect rect)
        {
            using var builder = new SKPathBuilder();
            builder.AddRect(rect.ToSKRect());

            StrokePath = builder.Detach();
            Bounds = rect;
        }
    }
}
