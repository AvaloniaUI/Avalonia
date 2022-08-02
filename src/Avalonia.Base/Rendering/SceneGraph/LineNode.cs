using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a line draw.
    /// </summary>
    internal class LineNode : BrushDrawOperation
    {
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
            IDisposable? aux = null)
            : base(LineBoundsHelper.CalculateBounds(p1, p2, pen), transform, aux)
        {
            Transform = transform;
            Pen = pen.ToImmutable();
            P1 = p1;
            P2 = p2;
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
            if (!Transform.HasInverse)
                return false;

            p *= Transform.Invert();

            var halfThickness = Pen.Thickness / 2;
            var minX = Math.Min(P1.X, P2.X) - halfThickness;
            var maxX = Math.Max(P1.X, P2.X) + halfThickness;
            var minY = Math.Min(P1.Y, P2.Y) - halfThickness;
            var maxY = Math.Max(P1.Y, P2.Y) + halfThickness;

            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
                return false;

            var a = P1;
            var b = P2;

            //If dot1 or dot2 is negative, then the angle between the perpendicular and the segment is obtuse.
            //The distance from a point to a straight line is defined as the
            //length of the vector formed by the point and the closest point of the segment

            Vector ap = p - a;
            var dot1 = Vector.Dot(b - a, ap);

            if (dot1 < 0)
                return ap.Length <= Pen.Thickness / 2;

            Vector bp = p - b;
            var dot2 = Vector.Dot(a - b, bp);

            if (dot2 < 0)
                return bp.Length <= halfThickness;

            var bXaX = b.X - a.X;
            var bYaY = b.Y - a.Y;

            var distance = (bXaX * (p.Y - a.Y) - bYaY * (p.X - a.X)) /
                           (Math.Sqrt(bXaX * bXaX + bYaY * bYaY));

            return Math.Abs(distance) <= halfThickness;
        }
    }
}
