using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a geometry clip push or pop.
    /// </summary>
    internal class GeometryClipNode : IDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryClipNode"/> class that represents a
        /// geometry clip push.
        /// </summary>
        /// <param name="transform">The current transform.</param>
        /// <param name="clip">The clip to push.</param>
        public GeometryClipNode(Matrix transform, IGeometryImpl clip)
        {
            Transform = transform;
            Clip = clip;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryClipNode"/> class that represents a
        /// geometry clip pop.
        /// </summary>
        public GeometryClipNode()
        {
        }

        /// <inheritdoc/>
        public Rect Bounds => Rect.Empty;

        /// <summary>
        /// Gets the clip to be pushed or null if the operation represents a pop.
        /// </summary>
        public IGeometryImpl? Clip { get; }

        /// <summary>
        /// Gets the transform with which the node will be drawn.
        /// </summary>
        public Matrix Transform { get; }

        /// <inheritdoc/>
        public bool HitTest(Point p) => false;

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="clip">The clip of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(Matrix transform, IGeometryImpl clip) => Transform == transform && Clip == clip;

        /// <inheritdoc/>
        public void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;

            if (Clip != null)
            {
                context.PushGeometryClip(Clip);
            }
            else
            {
                context.PopGeometryClip();
            }
        }
        public void Dispose()
        {
        }
    }
}
