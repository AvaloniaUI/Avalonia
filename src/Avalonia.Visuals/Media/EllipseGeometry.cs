// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
            var geometry = factory.CreateStreamGeometry();

            using (var ctx = geometry.Open())
            {
                var rect = Rect;
                double controlPointRatio = (Math.Sqrt(2) - 1) * 4 / 3;
                var center = rect.Center;
                var radius = new Vector(rect.Width / 2, rect.Height / 2);

                var x0 = center.X - radius.X;
                var x1 = center.X - (radius.X * controlPointRatio);
                var x2 = center.X;
                var x3 = center.X + (radius.X * controlPointRatio);
                var x4 = center.X + radius.X;

                var y0 = center.Y - radius.Y;
                var y1 = center.Y - (radius.Y * controlPointRatio);
                var y2 = center.Y;
                var y3 = center.Y + (radius.Y * controlPointRatio);
                var y4 = center.Y + radius.Y;

                ctx.BeginFigure(new Point(x2, y0), true);
                ctx.CubicBezierTo(new Point(x3, y0), new Point(x4, y1), new Point(x4, y2));
                ctx.CubicBezierTo(new Point(x4, y3), new Point(x3, y4), new Point(x2, y4));
                ctx.CubicBezierTo(new Point(x1, y4), new Point(x0, y3), new Point(x0, y2));
                ctx.CubicBezierTo(new Point(x0, y1), new Point(x1, y0), new Point(x2, y0));
                ctx.EndFigure(true);
            }

            return geometry;
        }
    }
}
