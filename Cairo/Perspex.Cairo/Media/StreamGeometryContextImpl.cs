// -----------------------------------------------------------------------
// <copyright file="StreamGeometryContextImpl.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media
{
    using Perspex.Media;
    using Perspex.Platform;
    using System.Collections.Generic;
    using Cairo = global::Cairo;

    public class StreamGeometryContextImpl : IStreamGeometryContextImpl
    {
        private Queue<GeometryOp> ops;
        private StreamGeometryImpl impl;
        public StreamGeometryContextImpl(Queue<GeometryOp> ops, StreamGeometryImpl imp)
        {
            this.ops = ops;
            this.impl = imp;
            points = new List<Point>();
        }

        private List<Point> points;

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            // TODO: Implement
            int i = 10;
            points.Add(point);
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            System.Diagnostics.Debug.WriteLine("IS filled {0}", isFilled);
            ops.Enqueue(new BeginOp { Point = startPoint, IsFilled = isFilled });
            points.Add(startPoint);
        }

        public void BezierTo(Point point1, Point point2, Point point3)
        {
            // TODO: Implement
            ops.Enqueue(new CurveToOp { Point = point1, Point2 = point2, Point3 = point3 });
            points.Add(point1);
            points.Add(point2);
            points.Add(point3);
        }

        public void LineTo(Point point)
        {
            ops.Enqueue(new LineToOp { Point = point });
            points.Add(point);
        }

        public void EndFigure(bool isClosed)
        {
            this.ops.Enqueue(new EndOp { IsClosed = isClosed });

            double maxX = 0;
            double maxY = 0;

            foreach (var p in this.points)
            {
                maxX = System.Math.Max(p.X, maxX);
                maxY = System.Math.Max(p.Y, maxY);
            }

            var context = new Cairo.Context(new Cairo.ImageSurface(Cairo.Format.Argb32, (int)maxX, (int)maxY));
            var clone = new Queue<GeometryOp>(this.ops);
            bool useFill = false;
            
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

                context.StrokePreserve();
            }

            var test = context.StrokeExtents();
            this.impl.Bounds = new Rect(test.X, test.Y, test.Width, test.Height);

            context.Dispose();
        }

        public void Dispose()
        {
            // TODO: Implement
        }
    }
}
