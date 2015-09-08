





namespace Perspex.Direct2D1.Media
{
    using Perspex.Media;
    using Perspex.Platform;
    using SharpDX.Direct2D1;
    using Splat;
    using D2DGeometry = SharpDX.Direct2D1.Geometry;

    /// <summary>
    /// A Direct2D implementation of a <see cref="StreamGeometry"/>.
    /// </summary>
    public class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
    {
        private PathGeometry path;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public StreamGeometryImpl()
        {
            Factory factory = Locator.Current.GetService<Factory>();
            this.path = new PathGeometry(factory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="geometry">An existing Direct2D <see cref="PathGeometry"/>.</param>
        protected StreamGeometryImpl(PathGeometry geometry)
        {
            this.path = geometry;
        }

        /// <inheritdoc/>
        public override Rect Bounds
        {
            get { return this.path.GetBounds().ToPerspex(); }
        }

        /// <inheritdoc/>
        public override D2DGeometry DefiningGeometry
        {
            get { return this.path; }
        }

        /// <summary>
        /// Clones the geometry.
        /// </summary>
        /// <returns>A cloned geometry.</returns>
        public IStreamGeometryImpl Clone()
        {
            Factory factory = Locator.Current.GetService<Factory>();
            var result = new PathGeometry(factory);
            var sink = result.Open();
            this.path.Stream(sink);
            sink.Close();
            return new StreamGeometryImpl(result);
        }

        /// <summary>
        /// Opens the geometry to start defining it.
        /// </summary>
        /// <returns>
        /// A <see cref="StreamGeometryContext"/> which can be used to define the geometry.
        /// </returns>
        public IStreamGeometryContextImpl Open()
        {
            return new StreamGeometryContextImpl(this.path.Open());
        }
    }
}
