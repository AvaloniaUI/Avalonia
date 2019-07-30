// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a rectangle draw.
    /// </summary>
    internal class RectangleNode : BrushDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="rect">The rectangle to draw.</param>
        /// <param name="cornerRadius">The rectangle corner radius.</param>
        /// <param name="childScenes">Child scenes for drawing visual brushes.</param>
        public RectangleNode(
            Matrix transform,
            IBrush brush,
            IPen pen,
            Rect rect,
            float cornerRadius,
            IDictionary<IVisual, Scene> childScenes = null)
            : base(rect, transform, pen)
        {
            Transform = transform;
            Brush = brush?.ToImmutable();
            Pen = pen?.ToImmutable();
            Rect = rect;
            CornerRadius = cornerRadius;
            ChildScenes = childScenes;
        }

        /// <summary>
        /// Gets the transform with which the node will be drawn.
        /// </summary>
        public Matrix Transform { get; }

        /// <summary>
        /// Gets the fill brush.
        /// </summary>
        public IBrush Brush { get; }

        /// <summary>
        /// Gets the stroke pen.
        /// </summary>
        public ImmutablePen Pen { get; }

        /// <summary>
        /// Gets the rectangle to draw.
        /// </summary>
        public Rect Rect { get; }

        /// <summary>
        /// Gets the rectangle corner radius.
        /// </summary>
        public float CornerRadius { get; }

        /// <inheritdoc/>
        public override IDictionary<IVisual, Scene> ChildScenes { get; }

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="brush">The fill of the other draw operation.</param>
        /// <param name="pen">The stroke of the other draw operation.</param>
        /// <param name="rect">The rectangle of the other draw operation.</param>
        /// <param name="cornerRadius">The rectangle corner radius of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(Matrix transform, IBrush brush, IPen pen, Rect rect, float cornerRadius)
        {
            return transform == Transform &&
                Equals(brush, Brush) &&
                Equals(Pen, pen) &&
                rect == Rect &&
                cornerRadius == CornerRadius;
        }

        /// <inheritdoc/>
        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;

            if (Brush != null)
            {
                context.FillRectangle(Brush, Rect, CornerRadius);
            }

            if (Pen != null)
            {
                context.DrawRectangle(Pen, Rect, CornerRadius);
            }
        }

        /// <inheritdoc/>
        public override bool HitTest(Point p)
        {
            // TODO: This doesn't respect CornerRadius yet.
            if (Transform.HasInverse)
            {
                p *= Transform.Invert();

                if (Brush != null)
                {
                    var rect = Rect.Inflate((Pen?.Thickness / 2) ?? 0);
                    return rect.Contains(p);
                }
                else
                {
                    var borderRect = Rect.Inflate((Pen?.Thickness / 2) ?? 0);
                    var emptyRect = Rect.Deflate((Pen?.Thickness / 2) ?? 0);
                    return borderRect.Contains(p) && !emptyRect.Contains(p);
                }
            }

            return false;
        }
    }
}
