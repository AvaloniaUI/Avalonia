using System;
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
        
        /// <summary>
        /// Defines the <see cref="RadiusX"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RadiusXProperty =
            AvaloniaProperty.Register<EllipseGeometry, double>(nameof(RadiusX));
        
        /// <summary>
        /// Defines the <see cref="RadiusY"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RadiusYProperty =
            AvaloniaProperty.Register<EllipseGeometry, double>(nameof(RadiusY));

        /// <summary>
        /// Defines the <see cref="Center"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> CenterProperty =
            AvaloniaProperty.Register<EllipseGeometry, Point>(nameof(Center));

        static EllipseGeometry()
        {
            AffectsGeometry(RectProperty, RadiusXProperty, RadiusYProperty, CenterProperty);
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
        /// <remarks>
        /// When set, this takes priority over the other properties that define an
        /// ellipse using a center point and X/Y-axis radii.
        /// </remarks>
        public Rect Rect
        {
            get => GetValue(RectProperty);
            set => SetValue(RectProperty, value);
        }

        /// <summary>
        /// Gets or sets a double that defines the radius in the X-axis of the ellipse.
        /// </summary>
        /// <remarks>
        /// In order for this property to be used, <see cref="Rect"/> must not be set
        /// (equal to the default <see cref="Avalonia.Rect"/> value).
        /// </remarks>
        public double RadiusX
        {
            get => GetValue(RadiusXProperty);
            set => SetValue(RadiusXProperty, value);
        }

        /// <summary>
        /// Gets or sets a double that defines the radius in the Y-axis of the ellipse.
        /// </summary>
        /// <remarks>
        /// In order for this property to be used, <see cref="Rect"/> must not be set
        /// (equal to the default <see cref="Avalonia.Rect"/> value).
        /// </remarks>
        public double RadiusY
        {
            get => GetValue(RadiusYProperty);
            set => SetValue(RadiusYProperty, value);
        }

        /// <summary>
        /// Gets or sets a point that defines the center of the ellipse.
        /// </summary>
        /// <remarks>
        /// In order for this property to be used, <see cref="Rect"/> must not be set
        /// (equal to the default <see cref="Avalonia.Rect"/> value).
        /// </remarks>
        public Point Center
        {
            get => GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            // Note that the ellipse properties are used in two modes:
            //
            //  1. Rect-only Mode:
            //     Directly set the rectangle bounds the ellipse will fill
            //
            //  2. Center + Radii Mode:
            //     Set a center-point and then X/Y-axis radii that are used to
            //     calculate the rectangle bounds the ellipse will fill.
            //     This is the only mode supported by WPF.
            //
            // Rendering the ellipse will only ever use one of these two modes
            // based on if the Rect property is set (not equal to default).
            //
            // This means it would normally be fine to copy ONLY the Rect property
            // when it is set. However, while it would render the same, it isn't
            // a true clone. We want to include all the properties here regardless
            // of the rendering mode that will eventually be used.
            return new EllipseGeometry()
            {
                Rect = Rect,
                RadiusX = RadiusX,
                RadiusY = RadiusY,
                Center = Center,
            };
        }

        /// <inheritdoc/>
        private protected sealed override IGeometryImpl? CreateDefiningGeometry()
        {
            var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

            if (Rect != default) return factory.CreateEllipseGeometry(Rect);
            
            var originX = Center.X - RadiusX;
            var originY = Center.Y - RadiusY;
            var width = RadiusX * 2;
            var height = RadiusY * 2;
            
            return factory.CreateEllipseGeometry(new Rect(originX, originY, width, height));
        }
    }
}
