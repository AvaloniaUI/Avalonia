// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Defines a geometric shape.
    /// </summary>    
    public abstract class Geometry : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="Transform"/> property.
        /// </summary>
        public static readonly StyledProperty<Transform> TransformProperty =
            AvaloniaProperty.Register<Geometry, Transform>("Transform");

        /// <summary>
        /// Initializes static members of the <see cref="Geometry"/> class.
        /// </summary>
        static Geometry()
        {
            TransformProperty.Changed.AddClassHandler<Geometry>(x => x.TransformChanged);
        }

        /// <summary>
        /// Gets the geometry's bounding rectangle.
        /// </summary>
        public Rect Bounds => PlatformImpl.Bounds;

        /// <summary>
        /// Gets the platform-specific implementation of the geometry.
        /// </summary>
        public virtual IGeometryImpl PlatformImpl
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets a transform to apply to the geometry.
        /// </summary>
        public Transform Transform
        {
            get { return GetValue(TransformProperty); }
            set { SetValue(TransformProperty, value); }
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
            return PlatformImpl.GetRenderBounds(strokeThickness);
        }

        /// <summary>
        /// Indicates whether the geometry's fill contains the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        public bool FillContains(Point point)
        {
            return PlatformImpl.FillContains(point);
        }

        /// <summary>
        /// Indicates whether the geometry's stroke contains the specified point.
        /// </summary>
        /// <param name="pen">The pen to use.</param>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        public bool StrokeContains(Pen pen, Point point)
        {
            return PlatformImpl.StrokeContains(pen, point);
        }

        private void TransformChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var transform = (Transform)e.NewValue;
            PlatformImpl = PlatformImpl.WithTransform(transform.Value);
        }
    }
}
