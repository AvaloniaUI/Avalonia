





namespace Perspex.Media
{
    using System;
    using Perspex.Platform;

    /// <summary>
    /// Defines a geometric shape.
    /// </summary>
    public abstract class Geometry : PerspexObject
    {
        /// <summary>
        /// Defines the <see cref="Transform"/> property.
        /// </summary>
        public static readonly PerspexProperty<Transform> TransformProperty =
            PerspexProperty.Register<Geometry, Transform>("Transform");

        /// <summary>
        /// Initializes static members of the <see cref="Geometry"/> class.
        /// </summary>
        static Geometry()
        {
            TransformProperty.Changed.Subscribe(x =>
            {
                ((Geometry)x.Sender).PlatformImpl.Transform = ((Transform)x.NewValue).Value;
            });
        }

        /// <summary>
        /// Gets the geometry's bounding rectangle.
        /// </summary>
        public abstract Rect Bounds
        {
            get;
        }

        /// <summary>
        /// Gets the platform-specific implementation of the geometry.
        /// </summary>
        public IGeometryImpl PlatformImpl
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets a transform to apply to the geometry.
        /// </summary>
        public Transform Transform
        {
            get { return this.GetValue(TransformProperty); }
            set { this.SetValue(TransformProperty, value); }
        }

        /// <summary>
        /// Clones the geometry.
        /// </summary>
        /// <returns>A cloned geometry.</returns>
        public abstract Geometry Clone();

        /// <summary>
        /// Gets the geometry's bounding rectangle with the specified stroke thickness.
        /// </summary>
        /// <param name="strokeThickness">The stroke thickness.</param>
        /// <returns>The bounding rectangle.</returns>
        public Rect GetRenderBounds(double strokeThickness)
        {
            return this.PlatformImpl.GetRenderBounds(strokeThickness);
        }
    }
}
