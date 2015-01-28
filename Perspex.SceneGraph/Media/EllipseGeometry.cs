// -----------------------------------------------------------------------
// <copyright file="EllipseGeometry.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Perspex.Platform;
    using Splat;

    public class EllipseGeometry : Geometry
    {
        public EllipseGeometry(Rect rect)
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
            IStreamGeometryImpl impl = factory.CreateStreamGeometry();

            using (IStreamGeometryContextImpl ctx = impl.Open())
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
                ctx.BezierTo(new Point(x3, y0), new Point(x4, y1), new Point(x4, y2));
                ctx.BezierTo(new Point(x4, y3), new Point(x3, y4), new Point(x2, y4));
                ctx.BezierTo(new Point(x1, y4), new Point(x0, y3), new Point(x0, y2));
                ctx.BezierTo(new Point(x0, y1), new Point(x1, y0), new Point(x2, y0));
                ctx.EndFigure(true);
            }

            this.PlatformImpl = impl;
        }

        public override Rect Bounds
        {
            get { return this.PlatformImpl.Bounds; }
        }

        public override Geometry Clone()
        {
            return new EllipseGeometry(this.Bounds);
        }
    }
}
