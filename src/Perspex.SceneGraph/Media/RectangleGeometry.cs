





namespace Perspex.Media
{
    using Perspex.Platform;
    using Splat;

    /// <summary>
    /// Represents the geometry of a rectangle.
    /// </summary>
    public class RectangleGeometry : Geometry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle bounds.</param>
        public RectangleGeometry(Rect rect)
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
            IStreamGeometryImpl impl = factory.CreateStreamGeometry();

            using (IStreamGeometryContextImpl context = impl.Open())
            {
                context.BeginFigure(rect.TopLeft, true);
                context.LineTo(rect.TopRight);
                context.LineTo(rect.BottomRight);
                context.LineTo(rect.BottomLeft);
                context.EndFigure(true);
            }

            this.PlatformImpl = impl;
        }

        /// <inheritdoc/>
        public override Rect Bounds
        {
            get { return this.PlatformImpl.Bounds; }
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new RectangleGeometry(this.Bounds);
        }
    }
}
