// -----------------------------------------------------------------------
// <copyright file="StreamGeometryContextImpl.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media
{
    using Perspex.Media;
    using Perspex.Platform;
    using System;
    using System.Collections.Generic;
    using Cairo = global::Cairo;

    public class StreamGeometryContextImpl : IStreamGeometryContextImpl
    {
        private StreamGeometryImpl impl;
        public StreamGeometryContextImpl(StreamGeometryImpl imp)
        {
            this.impl = imp;
            this.surf = new Cairo.ImageSurface(Cairo.Format.Argb32, 0, 0);
            this.context = new Cairo.Context(this.surf);
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            this.context.MoveTo(startPoint.ToCairo());
        }

        public void BezierTo(Point point1, Point point2, Point point3)
        {
            this.context.CurveTo(point1.ToCairo(), point2.ToCairo(), point3.ToCairo());
        }

        public void LineTo(Point point)
        {
            this.context.LineTo(point.ToCairo());
        }

        private Cairo.Context context;
        private Cairo.ImageSurface surf;

        public void EndFigure(bool isClosed)
        {
            if (isClosed)
                this.context.ClosePath();

            var extents = this.context.StrokeExtents();
            this.impl.Bounds = new Rect(extents.X, extents.Y, extents.Width, extents.Height);
            this.impl.Path = this.context.CopyPath();
        }

        public void Dispose()
        {
            context.Dispose();
            surf.Dispose();
        }
    }
}
