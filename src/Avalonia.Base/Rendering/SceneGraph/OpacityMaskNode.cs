using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents an opacity mask push or pop.
    /// </summary>
    internal class OpacityMaskNode : BrushDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpacityMaskNode"/> class that represents an
        /// opacity mask push.
        /// </summary>
        /// <param name="mask">The opacity mask to push.</param>
        /// <param name="bounds">The bounds of the mask.</param>
        public OpacityMaskNode(IImmutableBrush mask, Rect bounds)
            : base(default, Matrix.Identity, mask)
        {
            MaskBounds = bounds;
        }

        /// <summary>
        /// Gets the bounds of the opacity mask or null if the operation represents a pop.
        /// </summary>
        public Rect? MaskBounds { get; }


        /// <inheritdoc/>
        public override bool HitTest(Point p) => false;

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="mask">The opacity mask of the other draw operation.</param>
        /// <param name="bounds">The opacity mask bounds of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(IBrush? mask, Rect? bounds) => Equals(Brush, mask) && MaskBounds == bounds;

        /// <inheritdoc/>
        public override void Render(IDrawingContextImpl context)
        {
            context.PushOpacityMask(Brush!, MaskBounds!.Value);
        }
    }

    internal class OpacityMaskPopNode : DrawOperation
    {
        public OpacityMaskPopNode() : base(default, Matrix.Identity)
        {
        }

        public override bool HitTest(Point p) => false;

        public override void Render(IDrawingContextImpl context) => context.PopOpacityMask();
    }
}
