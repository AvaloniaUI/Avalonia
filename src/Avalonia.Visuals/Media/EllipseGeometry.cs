// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of an ellipse or circle.
    /// </summary>
    public class EllipseGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="Rect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect> RectProperty =
            AvaloniaProperty.Register<EllipseGeometry, Rect>(nameof(Rect));

        static EllipseGeometry()
        {
            AffectsGeometry(RectProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EllipseGeometry"/> class.
        /// </summary>
        public EllipseGeometry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EllipseGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle that the ellipse should fill.</param>
        public EllipseGeometry(Rect rect) : this()
        {
            Rect = rect;
        }

        /// <summary>
        /// Gets or sets a rect that defines the bounds of the ellipse.
        /// </summary>
        public Rect Rect
        {
            get => GetValue(RectProperty);
            set => SetValue(RectProperty, value);
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new EllipseGeometry(Rect);
        }

        /// <inheritdoc/>
        protected override IGeometryImpl CreateDefiningGeometry()
        {
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            return factory.CreateEllipseGeometry(Rect);
        }
    }
}
