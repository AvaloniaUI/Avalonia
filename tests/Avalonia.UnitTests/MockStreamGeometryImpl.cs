using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockStreamGeometryImpl : IStreamGeometryImpl, ITransformedGeometryImpl
    {
        private MockStreamGeometryContext _context;

        public MockStreamGeometryImpl()
        {
            Transform = Matrix.Identity;
            _context = new MockStreamGeometryContext();
        }

        public MockStreamGeometryImpl(Matrix transform)
        {
            Transform = transform;
            _context = new MockStreamGeometryContext();
        }

        private MockStreamGeometryImpl(Matrix transform, MockStreamGeometryContext context)
        {
            Transform = transform;
            _context = context;
        }

        public IGeometryImpl SourceGeometry { get; }

        public Rect Bounds => _context.CalculateBounds();

        public Matrix Transform { get; }

        public IStreamGeometryImpl Clone()
        {
            return this;
        }

        public void Dispose()
        {
        }

        public bool FillContains(Point point)
        {
            return _context.FillContains(point);
        }

        public bool StrokeContains(IPen pen, Point point)
        {
            return false;
        }

        public Rect GetRenderBounds(IPen pen) => Bounds;

        public IGeometryImpl Intersect(IGeometryImpl geometry)
        {
            return new MockStreamGeometryImpl(Transform);
        }

        public IStreamGeometryContextImpl Open()
        {
            return _context;
        }

        public ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            return new MockStreamGeometryImpl(transform, _context);
        }

        class MockStreamGeometryContext : IStreamGeometryContextImpl
        {
            private List<Point> points = new List<Point>();
            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
            {
            }

            public void BeginFigure(Point startPoint, bool isFilled)
            {
                points.Add(startPoint);
            }

            public Rect CalculateBounds()
            {
                var left = double.MaxValue;
                var right = double.MinValue;
                var top = double.MaxValue;
                var bottom = double.MinValue;

                foreach (var p in points)
                {
                    left = Math.Min(p.X, left);
                    right = Math.Max(p.X, right);
                    top = Math.Min(p.Y, top);
                    bottom = Math.Max(p.Y, bottom);
                }

                return new Rect(new Point(left, top), new Point(right, bottom));
            }

            public void CubicBezierTo(Point point1, Point point2, Point point3)
            {
            }

            public void Dispose()
            {
            }

            public void EndFigure(bool isClosed)
            {
            }

            public void LineTo(Point point)
            {
                points.Add(point);
            }

            public void QuadraticBezierTo(Point control, Point endPoint)
            {
                throw new NotImplementedException();
            }

            public void SetFillRule(FillRule fillRule)
            {
            }

            public bool FillContains(Point point)
            {
                // Use the algorithm from http://www.blackpawn.com/texts/pointinpoly/default.html
                // to determine if the point is in the geometry (since it will always be convex in this situation)
                for (int i = 0; i < points.Count; i++)
                {
                    var a = points[i];
                    var b = points[(i + 1) % points.Count];
                    var c = points[(i + 2) % points.Count];

                    Vector v0 = c - a;
                    Vector v1 = b - a;
                    Vector v2 = point - a;

                    var dot00 = v0 * v0;
                    var dot01 = v0 * v1;
                    var dot02 = v0 * v2;
                    var dot11 = v1 * v1;
                    var dot12 = v1 * v2;


                    var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
                    var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
                    var v = (dot00 * dot12 - dot01 * dot02) * invDenom;
                    if ((u >= 0) && (v >= 0) && (u + v < 1)) return true;
                }
                return false;
            }
        }
    }
}
