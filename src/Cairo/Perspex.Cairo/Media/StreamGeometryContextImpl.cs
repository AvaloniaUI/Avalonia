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
            this.impl.Operations.Enqueue(new BeginOp { Point = startPoint, IsFilled = isFilled });
        }

        public void BezierTo(Point point1, Point point2, Point point3)
        {
            this.impl.Operations.Enqueue(new CurveToOp { Point = point1, Point2 = point2, Point3 = point3 });
        }

        public void LineTo(Point point)
        {
            this.impl.Operations.Enqueue(new LineToOp { Point = point });
        }

        private Cairo.Context context;
        private Cairo.ImageSurface surf;

        public void EndFigure(bool isClosed)
        {
            this.impl.Operations.Enqueue(new EndOp { IsClosed = isClosed });
            
            var clone = new Queue<GeometryOp>(this.impl.Operations);
            
            while (clone.Count > 0)
            {
                var current = clone.Dequeue();

                if (current is BeginOp)
                {
                    var bo = current as BeginOp;
                    context.MoveTo(bo.Point.ToCairo());
                }
                else if (current is LineToOp)
                {
                    var lto = current as LineToOp;
                    context.LineTo(lto.Point.ToCairo());
                }
                else if (current is EndOp)
                {
                    if (((EndOp)current).IsClosed)
                        context.ClosePath();
                }
                else if (current is CurveToOp)
                {
                    var cto = current as CurveToOp;
                    context.CurveTo(cto.Point.ToCairo(), cto.Point2.ToCairo(), cto.Point3.ToCairo());
                }
            }

            var extents = context.StrokeExtents();
            this.impl.Bounds = new Rect(extents.X, extents.Y, extents.Width, extents.Height);
        }

        public void Dispose()
        {
            context.Dispose();
            surf.Dispose();
        }
    }
}
