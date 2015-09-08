





namespace Perspex.Direct2D1.Media
{
    using Perspex.Platform;
    using SharpDX.Direct2D1;
    using Splat;

    /// <summary>
    /// The platform-specific interface for <see cref="Perspex.Media.Geometry"/>.
    /// </summary>
    public abstract class GeometryImpl : IGeometryImpl
    {
        private TransformedGeometry transformed;

        /// <summary>
        /// Gets the geometry's bounding rectangle.
        /// </summary>
        public abstract Rect Bounds
        {
            get;
        }

        /// <summary>
        /// Gets the geomentry without any transforms applied.
        /// </summary>
        public abstract Geometry DefiningGeometry
        {
            get;
        }

        /// <summary>
        /// Gets the Direct2D <see cref="Geometry"/>.
        /// </summary>
        public Geometry Geometry
        {
            get { return this.transformed ?? this.DefiningGeometry; }
        }

        /// <summary>
        /// Gets or sets the transform for the geometry.
        /// </summary>
        public Matrix Transform
        {
            get
            {
                return this.transformed != null ?
                    this.transformed.Transform.ToPerspex() :
                    Matrix.Identity;
            }

            set
            {
                if (value != this.Transform)
                {
                    if (this.transformed != null)
                    {
                        this.transformed.Dispose();
                        this.transformed = null;
                    }

                    if (!value.IsIdentity)
                    {
                        Factory factory = Locator.Current.GetService<Factory>();
                        this.transformed = new TransformedGeometry(
                            factory,
                            this.DefiningGeometry,
                            value.ToDirect2D());
                    }
                }
            }
        }

        /// <summary>
        /// Gets the geometry's bounding rectangle with the specified stroke thickness.
        /// </summary>
        /// <param name="strokeThickness">The stroke thickness.</param>
        /// <returns>The bounding rectangle.</returns>
        public Rect GetRenderBounds(double strokeThickness)
        {
            if (this.transformed != null)
            {
                return this.transformed.GetWidenedBounds((float)strokeThickness).ToPerspex();
            }
            else
            {
                return this.DefiningGeometry.GetWidenedBounds((float)strokeThickness).ToPerspex();
            }
        }
    }
}
