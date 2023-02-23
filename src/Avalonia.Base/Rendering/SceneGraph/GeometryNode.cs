using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a geometry draw.
    /// </summary>
    internal class GeometryNode : BrushDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        /// <param name="aux">Auxiliary data required to draw the brush.</param>
        public GeometryNode(Matrix transform,
            IImmutableBrush? brush,
            IPen? pen,
            IGeometryImpl geometry)
            : base(geometry.GetRenderBounds(pen).CalculateBoundsWithLineCaps(pen), transform, brush)
        {
            Pen = pen?.ToImmutable();
            Geometry = geometry;
        }

        /// <summary>
        /// Gets the stroke pen.
        /// </summary>
        public ImmutablePen? Pen { get; }

        /// <summary>
        /// Gets the geometry to draw.
        /// </summary>
        public IGeometryImpl Geometry { get; }

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="brush">The fill of the other draw operation.</param>
        /// <param name="pen">The stroke of the other draw operation.</param>
        /// <param name="geometry">The geometry of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(Matrix transform, IBrush? brush, IPen? pen, IGeometryImpl geometry)
        {
            return transform == Transform &&
                   Equals(brush, Brush) &&
                   Equals(Pen, pen) &&
                   Equals(geometry, Geometry);
        }

        /// <inheritdoc/>
        public override void Render(IDrawingContextImpl context)
        {
            context.DrawGeometry(Brush, Pen, Geometry);
        }

        /// <inheritdoc/>
        public override bool HitTest(Point p)
        {
            return (Brush != null && Geometry.FillContains(p)) ||
                   (Pen != null && Geometry.StrokeContains(Pen, p));
        }
    }
}
