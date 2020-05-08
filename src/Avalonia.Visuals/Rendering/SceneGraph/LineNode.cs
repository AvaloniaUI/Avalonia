using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{

    /// <summary>
    /// A node in the scene graph which represents a line draw.
    /// </summary>
    internal class LineNode : BrushDrawOperation
    {

        const double degreeToRadians = Math.PI / 180.0;

        private static double CalculateAngle(Point p1, Point p2)
        {
            var xDiff = p2.X - p1.X;
            var yDiff = p2.Y - p1.Y;

            return Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
        }

        private static double CalculateOppSide(double angle, double hyp)
        {
            return Math.Sin(angle * degreeToRadians) * hyp;
        }

        private static double CalculateAdjSide(double angle, double hyp)
        {
            return Math.Cos(angle * degreeToRadians) * hyp;
        }

        static Rect CalculateBounds(Point p1, Point p2, double thickness, double angleToCorner)
        {
            var pts = TranslatePointsAlongTangent(p1, p2, angleToCorner + 90, thickness / 2);

            return new Rect(pts.p1, pts.p2);
        }

        static (Point p1, Point p2) TranslatePointsAlongTangent(Point p1, Point p2, double angle, double distance)
        {
            var xDiff = CalculateOppSide(angle, distance);
            var yDiff = CalculateAdjSide(angle, distance);

            var c1 = new Point(p1.X + xDiff, p1.Y - yDiff);
            var c2 = new Point(p1.X - xDiff, p1.Y + yDiff);

            var c3 = new Point(p2.X + xDiff, p2.Y - yDiff);
            var c4 = new Point(p2.X - xDiff, p2.Y + yDiff);

            var minX = Math.Min(c1.X, Math.Min(c2.X, Math.Min(c3.X, c4.X)));
            var minY = Math.Min(c1.Y, Math.Min(c2.Y, Math.Min(c3.Y, c4.Y)));
            var maxX = Math.Max(c1.X, Math.Max(c2.X, Math.Max(c3.X, c4.X)));
            var maxY = Math.Max(c1.Y, Math.Max(c2.Y, Math.Max(c3.Y, c4.Y)));

            return (new Point(minX, minY), new Point(maxX, maxY));
        }

        static Rect CalculateBounds(Point p1, Point p2, IPen p)
        {
            var angle = CalculateAngle(p1, p2);

            var angleToCorner = 90 - angle;

            if (p.LineCap != PenLineCap.Flat)
            {
                var pts = TranslatePointsAlongTangent(p1, p2, angleToCorner, p.Thickness / 2);

                return CalculateBounds(pts.p1, pts.p2, p.Thickness, angleToCorner);
            }
            else
            {
                return CalculateBounds(p1, p2, p.Thickness, angleToCorner);
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The start point of the line.</param>
        /// <param name="p2">The end point of the line.</param>
        /// <param name="childScenes">Child scenes for drawing visual brushes.</param>
        public LineNode(
            Matrix transform,
            IPen pen,
            Point p1,
            Point p2,
            IDictionary<IVisual, Scene> childScenes = null)
            : base(CalculateBounds(p1, p2, pen), transform, pen)
        {
            Transform = transform;
            Pen = pen?.ToImmutable();
            P1 = p1;
            P2 = p2;
            ChildScenes = childScenes;

            
        }

        

        /// <summary>
        /// Gets the transform with which the node will be drawn.
        /// </summary>
        public Matrix Transform { get; }

        /// <summary>
        /// Gets the stroke pen.
        /// </summary>
        public ImmutablePen Pen { get; }

        /// <summary>
        /// Gets the start point of the line.
        /// </summary>
        public Point P1 { get; }

        /// <summary>
        /// Gets the end point of the line.
        /// </summary>
        public Point P2 { get; }

        /// <inheritdoc/>
        public override IDictionary<IVisual, Scene> ChildScenes { get; }

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="pen">The stroke of the other draw operation.</param>
        /// <param name="p1">The start point of the other draw operation.</param>
        /// <param name="p2">The end point of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(Matrix transform, IPen pen, Point p1, Point p2)
        {
            return transform == Transform && Equals(Pen, pen) && p1 == P1 && p2 == P2;
        }

        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawLine(Pen, P1, P2);
        }

        public override bool HitTest(Point p)
        {
            // TODO: Implement line hit testing.
            return false;
        }
    }
}
