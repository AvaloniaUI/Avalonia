using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.EllipseGeometry"/>.
    /// </summary>
    internal class EllipseGeometryImpl : GeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public EllipseGeometryImpl(Rect rect)
            : base(CreateGeometry(rect))
        {
        }

        private static Geometry CreateGeometry(Rect rect)
        {
            var ellipse = new Ellipse(rect.Center.ToSharpDX(), (float)rect.Width / 2, (float)rect.Height / 2);
            return new EllipseGeometry(Direct2D1Platform.Direct2D1Factory, ellipse);
        }
    }
}
