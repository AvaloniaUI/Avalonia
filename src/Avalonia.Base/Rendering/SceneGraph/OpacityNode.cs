using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents an opacity push or pop.
    /// </summary>
    internal class OpacityNode : IDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpacityNode"/> class that represents an
        /// opacity push.
        /// </summary>
        /// <param name="opacity">The opacity to push.</param>
        /// <param name="bounds">The bounds.</param>
        public OpacityNode(double opacity, Rect bounds)
        {
            Opacity = opacity;
            Bounds = bounds;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpacityNode"/> class that represents an
        /// opacity pop.
        /// </summary>
        public OpacityNode()
        {
        }

        /// <inheritdoc/>
        public Rect Bounds { get; }

        /// <summary>
        /// Gets the opacity to be pushed or null if the operation represents a pop.
        /// </summary>
        public double? Opacity { get; }

        /// <inheritdoc/>
        public bool HitTest(Point p) => false;

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="opacity">The opacity of the other draw operation.</param>
        /// <param name="bounds">The bounds of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(double? opacity, Rect bounds) => Opacity == opacity && Bounds == bounds;

        /// <inheritdoc/>
        public void Render(IDrawingContextImpl context)
        {
            if (Opacity.HasValue)
            {
                context.PushOpacity(Opacity.Value, Bounds);
            }
            else
            {
                context.PopOpacity();
            }
        }

        public void Dispose()
        {
        }
    }
}
