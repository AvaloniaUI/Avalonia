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

        public Rect Rect
        {
            get => GetValue(RectProperty);
            set => SetValue(RectProperty, value);
        }

        static EllipseGeometry()
        {
            RectProperty.Changed.AddClassHandler<EllipseGeometry>(x => x.RectChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EllipseGeometry"/> class.
        /// </summary>
        public EllipseGeometry()
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            PlatformImpl = factory.CreateStreamGeometry();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EllipseGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle that the ellipse should fill.</param>
        public EllipseGeometry(Rect rect) : this()
        {
            Rect = rect;
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new EllipseGeometry(Rect);
        }

        private void RectChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var rect = (Rect)e.NewValue;
            using (var ctx = ((IStreamGeometryImpl)PlatformImpl).Open())
            {
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
        }
    }
}
